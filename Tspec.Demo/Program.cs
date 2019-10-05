using System;
using System.IO;
using System.Reflection;

namespace Tspec.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var spec = new Spec();
            spec.AddStepImplAssembly(Assembly.GetExecutingAssembly());
            spec.AddStepDefFile(new FileInfo("Demo.spc.md"));
            
            // spec.Dump(Console.Out);            

            var results = spec.Run();

            foreach (var result in results)
            {
                Console.WriteLine(result);
            }
        }
    }
}