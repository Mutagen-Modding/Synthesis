using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class CreateProfileRunnerTests
{
    [Theory, MutagenModAutoData]
    public async Task Typical(
        string profileName,
        string initialGroupName,
        DirectoryPath existingSettingsPath,
        CreateProfileRunnerContainer sutContainer)
    {
        var sut = sutContainer.Resolve().Value;
        sut.RunInternal(new CreateProfileCommand()
        {
            GameRelease = GameRelease.SkyrimSE,
            ProfileName = profileName,
            InitialGroupName = initialGroupName,
            SettingsFolderPath = existingSettingsPath
        });
    }
}