using CommandLine;

namespace Synthesis.Bethesda.Execution.Utility
{
    public interface IFormatCommandLine
    {
        string Format<T>(T obj);
    }

    public class FormatCommandLine : IFormatCommandLine
    {
        public string Format<T>(T obj)
        {
            return Parser.Default.FormatCommandLine<T>(obj);
        }
    }
}