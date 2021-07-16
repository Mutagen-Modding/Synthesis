using CommandLine;
using Mutagen.Bethesda;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running
{
    [Verb("run-patcher", HelpText = "Run the patcher")]
    public class RunPatcherPipelineInstructions
    {
        [Option('s', "SourcePath", Required = false, HelpText = "Optional path pointing to the previous patcher result to build onto.")]
        public string? SourcePath { get; set; }

        [Option('o', "OutputPath", Required = true, HelpText = "Path where the patcher should place its resulting file.")]
        public string OutputPath { get; set; } = string.Empty;

        [Option('g', "GameRelease", Required = true, HelpText = "GameRelease data folder is related to.")]
        public GameRelease GameRelease { get; set; }

        [Option('d', "DataFolderPath", Required = false, HelpText = "Path to the data folder.")]
        public string DataFolderPath { get; set; } = string.Empty;

        [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
        public string LoadOrderFilePath { get; set; } = string.Empty;

        [Option('p', "ProfileDefinitionPath", Required = true, HelpText = "Path to a specific profile or settings definition to run")]
        public string ProfileDefinitionPath { get; set; } = string.Empty;

        [Option('n', "ProfileName", Required = false, HelpText = "Nickname/GUID of profile to run if path is to a settings file with multiple profiles")]
        public string ProfileName { get; set; } = string.Empty;

        [Option('e', "ExtraDataFolder", Required = false, HelpText = "Path to where top level extra patcher data should be stored/read from.  Default is next to the exe")]
        public string? ExtraDataFolder { get; set; }

        [Option('r', "PersistencePath", Required = false, HelpText = "Path to the shared FormKey allocation state")]
        public string? PersistencePath { get; internal set; }

        [Option('m', "PersistenceMode", Required = false, HelpText = "Path to the Persistence state style to use")]
        public PersistenceMode? PersistenceMode { get; internal set; }

        public override string ToString()
        {
            return $"\n{nameof(RunSynthesisPatcher)} => \n"
                + $"  {nameof(SourcePath)} => {this.SourcePath} \n"
                + $"  {nameof(OutputPath)} => {this.OutputPath} \n"
                + $"  {nameof(GameRelease)} => {this.GameRelease} \n"
                + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
                + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath}\n"
                + $"  {nameof(ProfileDefinitionPath)} => {this.ProfileDefinitionPath} \n"
                + $"  {nameof(ProfileName)} => {this.ProfileName} \n"
                + $"  {nameof(ExtraDataFolder)} => {this.ExtraDataFolder}\n"
                + $"  {nameof(PersistencePath)} => {this.PersistencePath}\n"
                + $"  {nameof(PersistenceMode)} => {this.PersistenceMode}";
        }
    }
}
