using System.IO.Abstractions;
using Autofac;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class CreateProfileRunnerTests
{
    [Theory, MutagenModAutoData]
    public async Task Typical(
        IFileSystem fileSystem,
        string profileName,
        string initialGroupName,
        FilePath pipelineSettingsPath,
        PipelineSettingsV2Reader reader)
    {
        var cmd = new CreateProfileCommand()
        {
            GameRelease = GameRelease.SkyrimSE,
            ProfileName = profileName,
            InitialGroupName = initialGroupName,
            PipelineSettingsPath = pipelineSettingsPath
        };
        var b = new ContainerBuilder();
        b.RegisterModule(new CreateProfileModule(fileSystem, cmd));
        b.RegisterInstance(cmd).AsImplementedInterfaces();
        var cont = b.Build();
        await cont.Resolve<CreateProfileRunner>().RunInternal(cmd);
        var pipeSettings = reader.Read(pipelineSettingsPath);
        pipeSettings.Profiles.Select(x => x.Nickname).Should().Equal(profileName);
        pipeSettings.Profiles.SelectMany(x => x.Groups).Select(x => x.Name).Should().Equal(initialGroupName);
    }
}