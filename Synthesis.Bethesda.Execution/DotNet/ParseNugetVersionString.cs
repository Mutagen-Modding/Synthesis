using Synthesis.Bethesda.Execution.DotNet.Dto;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface IParseNugetVersionString
{
    DotNetVersion Parse(string str);
}

public class ParseNugetVersionString : IParseNugetVersionString
{
    public const int MinVersion = 5;
        
    public DotNetVersion Parse(string str)
    {
        var strSpan = str.AsSpan();
        var orig = strSpan;
        var indexOf = str.IndexOf('-');
        if (indexOf != -1)
        {
            strSpan = strSpan.Slice(0, indexOf);
        }
        if (Version.TryParse(strSpan, out var vers)
            && vers.Major >= MinVersion)
        {
            return new DotNetVersion(orig.ToString(), true);
        }
        return new DotNetVersion(orig.ToString(), false);
    }
}