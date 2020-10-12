using CommandLine;
using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.CLI
{
    [Verb("run-patcher", HelpText = "Run the patcher")]
    public class RunSynthesisMutagenPatcher
    {
        [Option('s', "SourcePath", Required = false, HelpText = "Optional path pointing to the previous patcher result to build onto.")]
        public string? SourcePath { get; set; }

        [Option('o', "OutputPath", Required = true, HelpText = "Path where the patcher should place its resulting file.")]
        public string OutputPath { get; set; } = string.Empty;

        [Option('g', "GameRelease", Required = true, HelpText = "GameRelease data folder is related to.")]
        public GameRelease GameRelease { get; set; }

        [Option('d', "DataFolderPath", Required = true, HelpText = "Path to the data folder.")]
        public string DataFolderPath { get; set; } = string.Empty;

        [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
        public string LoadOrderFilePath { get; set; } = string.Empty;

        [Option('e', "ExtraDataFolder", Required = true, HelpText = "Path to the extra data folder dedicated for a patcher")]
        public string? ExtraDataFolder { get; set; }

        public override string ToString()
        {
            return $"{nameof(RunSynthesisMutagenPatcher)} => \n"
                + $"  {nameof(SourcePath)} => {this.SourcePath} \n"
                + $"  {nameof(OutputPath)} => {this.OutputPath} \n"
                + $"  {nameof(GameRelease)} => {this.GameRelease} \n"
                + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
                + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath}\n"
                + $"  {nameof(ExtraDataFolder)} => {this.ExtraDataFolder}";
        }

        public static RunSynthesisMutagenPatcher Factory(RunSynthesisPatcher settings)
        {
            return new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = settings.DataFolderPath,
                GameRelease = settings.GameRelease,
                LoadOrderFilePath = settings.LoadOrderFilePath,
                OutputPath = settings.OutputPath,
                SourcePath = settings.SourcePath
            };
        }
    }
}
