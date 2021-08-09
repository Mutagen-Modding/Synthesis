using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface ILinesToReflectionConfigsParser
    {
        ReflectionSettingsConfigs Parse(IEnumerable<string> lines);
    }

    public class LinesToReflectionConfigsParser : ILinesToReflectionConfigsParser
    {
        public ReflectionSettingsConfigs Parse(IEnumerable<string> lines)
        {
            return JsonConvert.DeserializeObject<ReflectionSettingsConfigs>(
                string.Join(Environment.NewLine, lines))!;
        }
    }
}