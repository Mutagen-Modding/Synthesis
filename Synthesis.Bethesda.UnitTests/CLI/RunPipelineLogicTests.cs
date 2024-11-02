using System.IO.Abstractions;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.CreateNewPatcher;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class RunPipelineLogicTests
{
    [Theory, MutagenAutoData]
    public async Task Typical(
        IFileSystem fileSystem,
        string profileName,
        DirectoryPath patcherDir,
        DirectoryPath outputDir,
        GameEnvironmentState env)
    {
        var name = "Hello World";
        var result = await new CreateTemplatePatcherSolutionRunner(fileSystem).Run(new CreatePatcherCommand()
        {
            PatcherName = name,
            GameCategory = GameCategory.Skyrim,
            ParentDirectory = patcherDir
        });
        result.Should().Be(0);
        result = await RunPipelineLogic.Run(new RunPatcherPipelineCommand()
        {
            DataFolderPath = env.DataFolderPath,
            LoadOrderFilePath = env.LoadOrderFilePath,
            ProfileName = profileName,
            OutputDirectory = outputDir,
        }, fileSystem);
        result.Should().Be(0);
    }
}