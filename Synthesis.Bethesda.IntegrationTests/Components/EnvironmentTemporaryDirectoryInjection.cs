using Noggog;
using Noggog.IO;

namespace Synthesis.Bethesda.IntegrationTests.Components;

public class EnvironmentTemporaryDirectoryInjection : IEnvironmentTemporaryDirectoryProvider
{
    public required DirectoryPath Path { get; init; }
}