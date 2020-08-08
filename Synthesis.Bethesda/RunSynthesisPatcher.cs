using CommandLine;
using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Synthesis.Bethesda
{
    [Verb("run-patcher", HelpText = "Run the patcher")]
    public class RunSynthesisPatcher : IRunPipelineSettings
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
    }
}
