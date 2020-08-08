using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda
{
    public interface IRunPipelineSettings
    {
        string? SourcePath { get; }
        string OutputPath { get; }
        GameRelease GameRelease { get; }
        string DataFolderPath { get; }
        string LoadOrderFilePath { get; }
    }
}
