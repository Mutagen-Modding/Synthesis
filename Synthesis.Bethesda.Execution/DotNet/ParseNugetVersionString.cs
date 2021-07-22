using System;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IParseNugetVersionString
    {
        DotNetVersion Parse(ReadOnlySpan<char> str);
    }

    public class ParseNugetVersionString : IParseNugetVersionString
    {
        public const int MinVersion = 5;
        
        public DotNetVersion Parse(ReadOnlySpan<char> str)
        {
            var orig = str;
            var indexOf = str.IndexOf('-');
            if (indexOf != -1)
            {
                str = str.Slice(0, indexOf);
            }
            if (Version.TryParse(str, out var vers)
                && vers.Major >= MinVersion)
            {
                return new DotNetVersion(orig.ToString(), true);
            }
            return new DotNetVersion(orig.ToString(), false);
        }
    }
}