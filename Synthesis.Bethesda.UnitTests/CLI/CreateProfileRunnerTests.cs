using Autofac;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class CreateProfileRunnerTests
{
    [Theory, MutagenModAutoData]
    public async Task Typical(
        string profileName,
        string initialGroupName,
        DirectoryPath existingSettingsPath,
        PipelineSettingsPath pipelineSettingsPathProvider,
        PipelineSettingsV2Reader reader,
        CreateProfileModule createProfileRunnerModule)
    {
        var b = new ContainerBuilder();
        b.RegisterModule(createProfileRunnerModule);
        await b.Build().Resolve<CreateProfileRunner>().RunInternal(new CreateProfileCommand()
        {
            GameRelease = GameRelease.SkyrimSE,
            ProfileName = profileName,
            InitialGroupName = initialGroupName,
            SettingsFolderPath = existingSettingsPath
        });
        var pipelineSettingsPath = Path.Combine(existingSettingsPath, pipelineSettingsPathProvider.Name);
        var pipeSettings = reader.Read(pipelineSettingsPath);
        pipeSettings.Profiles.Select(x => x.Nickname).Should().Equal(profileName);
        pipeSettings.Profiles.SelectMany(x => x.Groups).Select(x => x.Name).Should().Equal(initialGroupName);
    }
}