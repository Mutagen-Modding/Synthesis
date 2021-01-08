using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda
{
    [Verb("settings-query-response", HelpText = "Response what style of settings the patcher supports.")]
    public class SettingsQueryResponse
    {
        [Option('t', "SettingsClassType", Required = false, HelpText = "Type of the settings class")]
        public string SettingsClassType { get; set; } = string.Empty;
    }
}
