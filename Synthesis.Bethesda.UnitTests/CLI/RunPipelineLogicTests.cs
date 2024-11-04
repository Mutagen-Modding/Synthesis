using System.IO.Abstractions;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.CreateNewPatcher;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class RunPipelineLogicTests
{
    [Theory, MutagenAutoData]
    public async Task Typical(
        IFileSystem fileSystem,
        string profileName,
        string initialGroupName,
        DirectoryPath patcherDir,
        DirectoryPath outputDir,
        GameEnvironmentState env,
        DirectoryPath existingSettingsPath,
        PipelineSettingsPath settingsNameProvider,
        CreateProfileRunnerContainer createProfileRunnerContainer)
    {
        var name = "Hello World";
        var result = await new CreateTemplatePatcherSolutionRunner(fileSystem).Run(new CreatePatcherCommand()
        {
            PatcherName = name,
            GameCategory = GameCategory.Skyrim,
            ParentDirectory = patcherDir
        });
        result.Should().Be(0);
        createProfileRunnerContainer.Resolve().Value.RunInternal(new CreateProfileCommand()
        {
            ProfileName = profileName,
            InitialGroupName = initialGroupName,
            SettingsFolderPath = existingSettingsPath,
            GameRelease = GameRelease.SkyrimSE
        });
        result = await RunPipelineLogic.Run(new RunPatcherPipelineCommand()
        {
            DataFolderPath = env.DataFolderPath,
            LoadOrderFilePath = env.LoadOrderFilePath,
            ProfileName = profileName,
            OutputDirectory = outputDir,
            ProfileDefinitionPath = Path.Combine(existingSettingsPath, settingsNameProvider.Name)
        }, fileSystem);
        result.Should().Be(0);
    }
}