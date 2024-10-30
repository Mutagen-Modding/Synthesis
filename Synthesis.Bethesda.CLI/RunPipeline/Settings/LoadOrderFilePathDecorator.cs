using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI.RunPipeline.Settings;

public class LoadOrderFilePathDecorator : IPluginListingsPathContext
{
    public IPluginListingsPathContext ListingsPathContext { get; }
    public RunPatcherPipelineCommand Command { get; }

    public FilePath Path => Command.LoadOrderFilePath == default
        ? ListingsPathContext.Path
        : Command.LoadOrderFilePath;

    public LoadOrderFilePathDecorator(
        IPluginListingsPathContext listingsPathContext,
        RunPatcherPipelineCommand command)
    {
        ListingsPathContext = listingsPathContext;
        Command = command;
    }
}