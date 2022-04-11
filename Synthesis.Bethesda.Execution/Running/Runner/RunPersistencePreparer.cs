using System;
using System.IO.Abstractions;
using Mutagen.Bethesda.Plugins.Allocators;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunPersistencePreparer
{
    string? Prepare(PersistenceMode persistenceMode, string? persistencePath);
}

public class RunPersistencePreparer : IRunPersistencePreparer
{
    private readonly IFileSystem _fileSystem;

    public RunPersistencePreparer(
        IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public string? Prepare(PersistenceMode persistenceMode, string? persistencePath)
    {
        switch (persistenceMode)
        {
            case PersistenceMode.None:
                return null;
            case PersistenceMode.Text:
                TextFileSharedFormKeyAllocator.Initialize(persistencePath ?? throw new ArgumentNullException("Persistence mode specified, but no path provided"), _fileSystem);
                break;
            default:
                throw new NotImplementedException();
        }

        return persistencePath;
    }
}