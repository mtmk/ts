using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Tspec.WebEditor;

namespace Tspec.WebApp.Data
{
    public class IndexBase:ComponentBase
    {
        
        public string Results { get; set; }
        
        public string Value { get; set; } = @"# Specification Heading

This is an executable specification file. This file follows markdown syntax.
Every heading in this file denotes a scenario. Every bulleted point denotes a step.

* Vowels in English language are ""aeiou"".

## Vowel counts in single word

tags: single word

* The word ""gauge"" has ""3"" vowels.

## Vowel counts in multiple word

This is the second scenario in this specification
Here's a step that takes a table

* Almost all words have vowels 

    |Word  |Vowel Count|
    |------|-----------|
    |Gauge |3          |
    |Mingle|2          |
    |Snap  |1          |
    |GoCD  |1          |
    |Rhythm|0          |
";
        
        public EditorModel _editorModel { get; set; }
    
    
        public MonacoEditor _editor;
    

        protected override async Task OnInitializedAsync()
        {
            _editorModel = new EditorModel
            {
                Options =
                {
                    Language = "markdown",
                    Theme = "vs-dark",
                    Value = Value
                }
            };

            await base.OnInitializedAsync();
        }
    
        public async Task ChangeTheme(ChangeEventArgs e)
        {
            Console.WriteLine($"setting theme to: {e.Value}");
            await _editor.SetTheme(e.Value.ToString());
        }

        public async Task SetValue()
        {
            Console.WriteLine($"setting value to: {Value}");
            await _editor.SetValue(Value);
        }

        public async Task GetValue()
        {
            var val = await _editor.GetValue();
            Value = val;
            Console.WriteLine($"value is: {val}");
        }

        public async Task RunSpec()
        {
            var val = await _editor.GetValue();
            var spec = new Spec();
            spec.AddStepImplAssembly(Assembly.GetExecutingAssembly());
            spec.AddStepDefText(val);
            var results = spec.Run();

            Results = "";
            foreach (var result in results)
            {
                Results += $"{result}\n";
                Console.WriteLine(result);
            }
            
            StateHasChanged();
        }
    }
}