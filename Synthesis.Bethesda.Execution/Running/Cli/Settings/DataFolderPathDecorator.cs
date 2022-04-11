using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.Execution.Running.Cli.Settings;

public class DataFolderPathDecorator : IDataDirectoryProvider
{
    public IDataDirectoryProvider DataDirectoryProvider { get; }
    public RunPatcherPipelineInstructions Instructions { get; }

    public DirectoryPath Path => GetPath();
        
    public DataFolderPathDecorator(
        IDataDirectoryProvider dataDirectoryProvider,
        RunPatcherPipelineInstructions instructions)
    {
        DataDirectoryProvider = dataDirectoryProvider;
        Instructions = instructions;
    }

    private DirectoryPath GetPath()
    {
        if (Instructions.DataFolderPath != default(DirectoryPath))
        {
            return Instructions.DataFolderPath;
        }

        return DataDirectoryProvider.Path;
    }
}