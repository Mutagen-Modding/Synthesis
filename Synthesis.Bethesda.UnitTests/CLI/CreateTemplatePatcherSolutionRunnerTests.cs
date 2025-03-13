using System.IO.Abstractions;
using Shouldly;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.CreateTemplatePatcher;
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
        var result = await sut.Run(new CreateTemplatePatcherCommand()
        {
            PatcherName = name,
            GameCategory = GameCategory.Skyrim,
            ParentDirectory = existingDir
        });
        result.ShouldBe(0);
        fileSystem.File.Exists(Path.Combine(existingDir, $"{name}.sln")).ShouldBeTrue();
        fileSystem.File.Exists(Path.Combine(existingDir, name, $"{name}.csproj")).ShouldBeTrue();
    }
}