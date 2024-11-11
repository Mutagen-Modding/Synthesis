using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("run-pipeline", HelpText = "Run the patcher pipeline")]
[ExcludeFromCodeCoverage]
public class RunPatcherPipelineCommand : 
    ISettingsFolderProvider,
    IExecutionParametersSettingsProvider
{
    [Option('o', "OutputDirectory", Required = true, HelpText = "Path where the patcher should place its resulting file(s).")]
    public required string OutputDirectory { get; set; }

    [Option('d', "DataFolderPath", Required = false, HelpText = "Path to the data folder.")]
    public string? DataFolderPath { get; set; }

    [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
    public string? LoadOrderFilePath { get; set; }

    [Option('s', "SettingsFolderPath",
        HelpText = "Path to the folder containing the PipelineSettings.json to be adjusted",
        Required = true)]
    public string SettingsFolderPath { get; set; } = string.Empty;

    [Option('p', "ProfileIdentifier", Required = false, HelpText = "Nickname/GUID of profile to run if path is to a settings file with multiple profiles")]
    public string? ProfileIdentifier { get; set; }

    [Option('e', "ExtraDataFolder", Required = false, HelpText = "Path to where top level extra patcher data should be stored/read from.  Default is next to the exe")]
    public string? ExtraDataFolder { get; set; }

    [Option('r', "PersistencePath", Required = false, HelpText = "Path to the shared FormKey allocation state")]
    public string? PersistencePath { get; internal set; }

    [Option('m', "PersistenceMode", Required = false, HelpText = "Path to the Persistence state style to use")]
    public PersistenceMode? PersistenceMode { get; internal set; }
    
    [Option('t', "TargetRuntime", Required = false, HelpText = "Target runtime to specify explicitly")]
    public string? TargetRuntime { get; set; }

    public override string ToString()
    {
        return $"\n{nameof(RunSynthesisPatcher)} => \n"
               + $"  {nameof(OutputDirectory)} => {this.OutputDirectory} \n"
               + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
               + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath}\n"
               + $"  {nameof(SettingsFolderPath)} => {this.SettingsFolderPath} \n"
               + $"  {nameof(ProfileIdentifier)} => {this.ProfileIdentifier} \n"
               + $"  {nameof(ExtraDataFolder)} => {this.ExtraDataFolder}\n"
               + $"  {nameof(PersistencePath)} => {this.PersistencePath}\n"
               + $"  {nameof(PersistenceMode)} => {this.PersistenceMode}\n"
               + $"  {nameof(TargetRuntime)} => {this.TargetRuntime}";
    }

    DirectoryPath ISettingsFolderProvider.SettingsFolder => SettingsFolderPath;
}
