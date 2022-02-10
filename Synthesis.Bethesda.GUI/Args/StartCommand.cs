using CommandLine;

namespace Synthesis.Bethesda.GUI.Args;

[Verb("start", HelpText = "Start Synthesis")]
public class StartCommand
{
    [Option('p', "Profile", Required = false, HelpText = "Profile to start on opening")]
    public string? Profile { get; set; }
}