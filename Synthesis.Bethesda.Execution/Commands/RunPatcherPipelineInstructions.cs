using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("run-pipeline", HelpText = "Run the patcher pipeline")]
[ExcludeFromCodeCoverage]
public class RunPatcherPipelineInstructions :
    IProfileDefinitionPathProvider,
    IProfileNameProvider,
    IExecutionParametersSettingsProvider,
    IDataDirectoryProvider
{
    [Option('s', "SourcePath", Required = false, HelpText = "Optional path pointing to the previous patcher result to build onto.")]
    public FilePath? SourcePath { get; set; }

    [Option('o', "OutputDirectory", Required = true, HelpText = "Path where the patcher should place its resulting file(s).")]
    public DirectoryPath OutputDirectory { get; set; }

    [Option('d', "DataFolderPath", Required = false, HelpText = "Path to the data folder.")]
    public DirectoryPath DataFolderPath { get; set; } = string.Empty;

    [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
    public FilePath LoadOrderFilePath { get; set; } = string.Empty;

    [Option('p', "ProfileDefinitionPath", Required = true, HelpText = "Path to a specific profile or settings definition to run")]
    public FilePath ProfileDefinitionPath { get; set; } = string.Empty;

    [Option('n', "ProfileName", Required = false, HelpText = "Nickname/GUID of profile to run if path is to a settings file with multiple profiles")]
    public string ProfileName { get; set; } = string.Empty;

    [Option('e', "ExtraDataFolder", Required = false, HelpText = "Path to where top level extra patcher data should be stored/read from.  Default is next to the exe")]
    public DirectoryPath? ExtraDataFolder { get; set; }

    [Option('r', "PersistencePath", Required = false, HelpText = "Path to the shared FormKey allocation state")]
    public string? PersistencePath { get; internal set; }

    [Option('m', "PersistenceMode", Required = false, HelpText = "Path to the Persistence state style to use")]
    public PersistenceMode? PersistenceMode { get; internal set; }
    
    [Option('t', "TargetRuntime", Required = false, HelpText = "Target runtime to specify explicitly")]
    public string? TargetRuntime { get; set; }

    public override string ToString()
    {
        return $"\n{nameof(RunSynthesisPatcher)} => \n"
               + $"  {nameof(SourcePath)} => {this.SourcePath} \n"
               + $"  {nameof(OutputDirectory)} => {this.OutputDirectory} \n"
               + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
               + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath}\n"
               + $"  {nameof(ProfileDefinitionPath)} => {this.ProfileDefinitionPath} \n"
               + $"  {nameof(ProfileName)} => {this.ProfileName} \n"
               + $"  {nameof(ExtraDataFolder)} => {this.ExtraDataFolder}\n"
               + $"  {nameof(PersistencePath)} => {this.PersistencePath}\n"
               + $"  {nameof(PersistenceMode)} => {this.PersistenceMode}\n"
               + $"  {nameof(TargetRuntime)} => {this.TargetRuntime}";
    }
    
    FilePath IProfileDefinitionPathProvider.Path => ProfileDefinitionPath;
    string IProfileNameProvider.Name => ProfileName;
    DirectoryPath IDataDirectoryProvider.Path => DataFolderPath;
}