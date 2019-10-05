namespace Tspec
{
    public class Result
    {
        public bool Success { get; internal set; }
        public string Text { get; internal set; }
        public string Error { get; internal set; }

        public override string ToString()
        {
            var r = Success ? "PASS" : "FAIL";
            var pad = Error == null ? "" : ": ";
            return $"{r} ({Text}){pad}{Error}";
        }
    }
}