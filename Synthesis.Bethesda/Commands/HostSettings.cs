using CommandLine;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Commands;

[Verb("host-settings", HelpText = "Instructs Synthesis setting host project to display a patcher's settings")]
public class HostSettings
{
    [Option('p', "PatcherPath", Required = true, HelpText = "Path to the patcher exe")]
    public string PatcherPath { get; set; } = string.Empty;

    [Option('n', "PatcherName", Required = true, HelpText = "Pather name")]
    public string PatcherName { get; set; } = string.Empty;

    [Option('y', "Top", Required = false, HelpText = "Top location to consider when positioning")]
    public int Top { get; set; }

    [Option('x', "Left", Required = false, HelpText = "Left location to consider when positioning")]
    public int Left { get; set; }

    [Option('w', "Width", Required = false, HelpText = "Width consider when positioning")]
    public int Width { get; set; }

    [Option('h', "Height", Required = false, HelpText = "Height to consider when positioning")]
    public int Height { get; set; }

    [Option('g', "GameRelease", Required = true, HelpText = "GameRelease data folder is related to.")]
    public GameRelease GameRelease { get; set; }

    [Option('d', "DataFolderPath", Required = true, HelpText = "Path to the data folder.")]
    public string DataFolderPath { get; set; } = string.Empty;

    [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
    public string LoadOrderFilePath { get; set; } = string.Empty;
}