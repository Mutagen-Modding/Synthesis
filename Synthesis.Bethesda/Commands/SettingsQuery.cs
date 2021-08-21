using CommandLine;

namespace Synthesis.Bethesda.Commands
{
    [Verb("settings-query", HelpText = "Query to check what style of settings the patcher supports.")]
    public record SettingsQuery
    {
    }
}
