using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Commands;

public interface IBaseRunArgs
{
    GameRelease GameRelease { get; }
    string DataFolderPath { get; }
    string LoadOrderFilePath { get; }
    string? ModKey { get; }
    bool LoadOrderIncludesCreationClub { get; }
}