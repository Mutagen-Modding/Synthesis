using Newtonsoft.Json;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.Settings.Json;

public interface ILinesToReflectionConfigsParser
{
    ReflectionSettingsConfigs Parse(IEnumerable<string> lines);
}

public class LinesToReflectionConfigsParser : ILinesToReflectionConfigsParser
{
    private const string Starter = "{\"Configs";
    
    private string RemoveAnsiCode(string str)
    {
        var indexOf = str.IndexOf(Starter, StringComparison.OrdinalIgnoreCase);
        if (indexOf == -1) return str;
        return str.Substring(indexOf);
    }
    
    public ReflectionSettingsConfigs Parse(IEnumerable<string> lines)
    {
        var convertedLines = lines
            .SkipWhile(l => !l.Contains(Starter, StringComparison.OrdinalIgnoreCase))
            .Select(RemoveAnsiCode);
        var str = string.Join(Environment.NewLine, convertedLines);
        return JsonConvert.DeserializeObject<ReflectionSettingsConfigs>(str)!;
    }
}