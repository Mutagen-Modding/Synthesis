using CommandLine;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Commands
{
    [Verb("check-runnability", HelpText = "Check the current state and see if the patcher thinks it can run")]
    public record CheckRunnability
    {
        [Option('g', "GameRelease", Required = true, HelpText = "GameRelease data folder is related to.")]
        public GameRelease GameRelease { get; set; }

        [Option('d', "DataFolderPath", Required = true, HelpText = "Path to the data folder.")]
        public string DataFolderPath { get; set; } = string.Empty;

        [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
        public string LoadOrderFilePath { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{nameof(CheckRunnability)} => \n"
                + $"  {nameof(GameRelease)} => {this.GameRelease} \n"
                + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
                + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath}";
        }
    }
}
