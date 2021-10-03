using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.Execution.Running.Cli.Settings
{
    public class LoadOrderFilePathDecorator : IPluginListingsPathProvider
    {
        public IPluginListingsPathProvider Provider { get; }
        public RunPatcherPipelineInstructions Instructions { get; }

        public FilePath Path => Instructions.LoadOrderFilePath == default
            ? Provider.Path
            : Instructions.LoadOrderFilePath;

        public LoadOrderFilePathDecorator(
            IPluginListingsPathProvider provider,
            RunPatcherPipelineInstructions instructions)
        {
            Provider = provider;
            Instructions = instructions;
        }
    }
}