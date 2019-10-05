using System;

namespace Tspec
{
    public class StepAttribute : Attribute
    {
        public StepAttribute(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; }
    }
}