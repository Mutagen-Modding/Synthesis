using System.IO.Abstractions;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.CreateNewPatcher;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class CreateTemplatePatcherSolutionRunnerTests
{
    [Theory, MutagenAutoData]
    public async Task Typical(
        IFileSystem fileSystem,
        DirectoryPath existingDir,
        CreateTemplatePatcherSolutionRunner sut)
    {
        var name = "Hello World";
        var result = await sut.Run(new CreatePatcherCommand()
        {
            PatcherName = name,
            GameCategory = GameCategory.Skyrim,
            ParentDirectory = existingDir
        });
        result.Should().Be(0);
        fileSystem.File.Exists(Path.Combine(existingDir, $"{name}.sln")).Should().BeTrue();
        fileSystem.File.Exists(Path.Combine(existingDir, name, $"{name}.csproj")).Should().BeTrue();
    }
}