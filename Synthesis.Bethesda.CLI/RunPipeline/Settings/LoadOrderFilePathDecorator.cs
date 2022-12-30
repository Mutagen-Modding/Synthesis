using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI.RunPipeline.Settings;

public class LoadOrderFilePathDecorator : IPluginListingsPathContext
{
    public IPluginListingsPathContext ListingsPathContext { get; }
    public RunPatcherPipelineInstructions Instructions { get; }

    public FilePath Path => Instructions.LoadOrderFilePath == default
        ? ListingsPathContext.Path
        : Instructions.LoadOrderFilePath;

    public LoadOrderFilePathDecorator(
        IPluginListingsPathContext listingsPathContext,
        RunPatcherPipelineInstructions instructions)
    {
        ListingsPathContext = listingsPathContext;
        Instructions = instructions;
    }
}