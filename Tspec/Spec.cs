using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Tspec
{
    public class Spec
    {
        private readonly List<SpecImpl> _impls = new List<SpecImpl>();
        private readonly List<SpecDef> _defs = new List<SpecDef>();

        public void AddStepImplAssembly(Assembly assembly)
        {
            _impls.AddRange(FindIn(assembly));
        }

        public void AddStepDefFile(FileInfo file)
        {
            using (var text = file.OpenText())
                _defs.AddRange(FindIn(text));
        }
        public void AddStepDefText(string str)
        {
            using (var text = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(str))))
                _defs.AddRange(FindIn(text));
        }
        
        private IEnumerable<SpecImpl> FindIn(Assembly assembly) => assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes().Any(a => a is StepAttribute))
            .Select(m =>
            {
                var pattern = m.GetCustomAttribute<StepAttribute>().Pattern;
                foreach (var p in m.GetParameters())
                {
                    var t = p.ParameterType;

                    var exp = t == typeof(int) ||
                              t == typeof(long) ||
                              t == typeof(short) ||
                              t == typeof(byte) ||
                              t == typeof(float) ||
                              t == typeof(double) ? @"[\d\.]+"
                        : t == typeof(string) ? @"(?:[^""\\]|\\.)*"
                        : t == typeof(char) ? @"(?:\\""|[^""])"
                        : null;

                    if (exp != null)
                    {
                        pattern = pattern.Replace($"<{p.Name}>", $"\"(?<{p.Name}>{exp})\"");
                    }
                    else
                    {
                        pattern = pattern.Replace($"<{p.Name}>", "");
                    }

                    pattern = pattern.Trim();
                }

                return new SpecImpl
                {
                    Method = m,
                    Pattern = pattern,
                };
            });

        private IEnumerable<SpecDef> FindIn(StreamReader text)
        {
            SpecDef current = null;
            while (true)
            {
                var line = text.ReadLine();
                if (line == null) break;

                // Any line starting with '*' is definition
                if (Regex.IsMatch(line, @"^\s*\*\s*.+"))
                {
                    current = new SpecDef {Text = Regex.Match(line, @"^\s*\*(.+?)$").Groups[1].Value.Trim()};
                    yield return current;
                }

                // Add any tables to the current spec
                if (current != null && Regex.IsMatch(line, @"^\s*\|"))
                {
                    var strings = line.Split('|');

                    // not a table
                    if (strings.Length < 3) continue;

                    // remove first and last elements and trim
                    var values = strings.Skip(1).Take(strings.Length - 2)
                        .Select(x => x.Trim()).ToArray();

                    // ignore table lines
                    if (values.All(s => s.All(c => c == '-'))) continue;

                    if (current.Table == null)
                    {
                        // No table yet. Assume header
                        current.Table = new Table {Columns = values};
                    }
                    else
                    {
                        // ..and the values
                        current.Table.AddRow(values);
                    }
                }
            }
        }

        public IEnumerable<Result> Run()
        {
            var stepObjects = new Dictionary<Type, object>();

            foreach (var specDef in _defs)
            {
                var step = _impls.FirstOrDefault(s => Regex.IsMatch(specDef.Text, s.Pattern));
                if (step == null)
                {
                    Console.WriteLine($"ERROR: Did not match: {specDef.Text}");
                    continue;
                }

                var match = Regex.Match(specDef.Text, step.Pattern);

                var t = step.Method.DeclaringType;
                if (!stepObjects.TryGetValue(t, out var obj))
                {
                    stepObjects[t] = obj = Activator.CreateInstance(t);
                }

                var parameters = new List<object>();

                foreach (var parameterInfo in step.Method.GetParameters())
                {
                    if (parameterInfo.ParameterType == typeof(Table))
                    {
                        parameters.Add(specDef.Table);
                    }
                    else
                    {
                        var strValue = match.Groups[parameterInfo.Name].Value;
                        var value = Convert.ChangeType(strValue, parameterInfo.ParameterType);
                        parameters.Add(value);
                    }
                }

                Exception exception = null;
                try
                {
                    step.Method.Invoke(obj, parameters.ToArray());
                }
                catch (Exception e)
                {
                    exception = e;
                }

                yield return new Result
                {
                    Success = exception == null,
                    Text = specDef.Text,
                    Error = exception?.GetBaseException().Message,
                };
            }
        }

        public void Dump(TextWriter @out)
        {
            foreach (var impl in _impls)
            {
                @out.WriteLine(impl);
            }

            foreach (var def in _defs)
            {
                @out.WriteLine(def);
                if (def.Table != null) @out.WriteLine(def.Table);
            }
        }
    }
}