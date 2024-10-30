using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI.RunPipeline.Settings;

public class DataFolderPathDecorator : IDataDirectoryProvider
{
    public IDataDirectoryProvider DataDirectoryProvider { get; }
    public RunPatcherPipelineCommand Command { get; }

    public DirectoryPath Path => GetPath();
        
    public DataFolderPathDecorator(
        IDataDirectoryProvider dataDirectoryProvider,
        RunPatcherPipelineCommand command)
    {
        DataDirectoryProvider = dataDirectoryProvider;
        Command = command;
    }

    private DirectoryPath GetPath()
    {
        if (Command.DataFolderPath != default(DirectoryPath))
        {
            return Command.DataFolderPath;
        }

        return DataDirectoryProvider.Path;
    }
}