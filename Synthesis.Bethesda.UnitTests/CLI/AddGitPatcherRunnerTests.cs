using System.IO.Abstractions;
using Autofac;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog.IO;
using Noggog.Testing.AutoFixture;
using Synthesis.Bethesda.CLI.AddGitPatcher;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Profile.Services;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class AddGitPatcherRunnerTests
{
    [Theory, MutagenModAutoData(FileSystem: TargetFileSystem.Real)]
    public async Task Typical(
        IFileSystem fileSystem,
        string profileName,
        string patcherNickname,
        string groupName,
        PipelineSettingsPath pipelineSettingsPathProvider,
        PipelineSettingsV2Reader reader)
    {
        using var settingsFolder = TempFolder.Factory();
        var createProfileCmd = new CreateProfileCommand()
        {
            ProfileName = profileName,
            InitialGroupName = groupName,
            SettingsFolderPath = settingsFolder.Dir,
            GameRelease = GameRelease.SkyrimSE
        };
        var repoPath = "https://github.com/Synthesis-Collective/facefixer";
        var b = new ContainerBuilder();
        b.RegisterModule(new CreateProfileModule(fileSystem, createProfileCmd));
        b.RegisterInstance(createProfileCmd).AsImplementedInterfaces();
        await b.Build().Resolve<CreateProfileRunner>().RunInternal(createProfileCmd);
        var pipelineSettingsPath = Path.Combine(settingsFolder.Dir, pipelineSettingsPathProvider.Name);
        var pipeSettings = reader.Read(pipelineSettingsPath);

        var addGitPatcherCmd = new AddGitPatcherCommand()
        {
            Nickname = patcherNickname,
            GroupName = groupName,
            ProfileIdentifier = profileName,
            SettingsFolderPath = settingsFolder.Dir,
            GitRepoAddress = repoPath,
            ProjectSubpath = Path.Combine("FaceFixer", "FaceFixer.csproj"),
        };
        b = new ContainerBuilder();
        b.RegisterModule(new AddGitPatcherModule(fileSystem, addGitPatcherCmd));
        b.RegisterInstance(addGitPatcherCmd).AsImplementedInterfaces();
        b.RegisterInstance(new ProfileIdentifier(pipeSettings.Profiles.First().ID)).AsImplementedInterfaces();
        var cont = b.Build();
        var profileDirs = cont.Resolve<IProfileDirectories>();
        
        try
        {
            await cont.Resolve<AddGitPatcherRunner>().Add(addGitPatcherCmd);
            pipeSettings = reader.Read(pipelineSettingsPath);
            var patcher = pipeSettings.Profiles.First().Groups.First().Patchers.First() as GithubPatcherSettings;
            patcher!.Nickname.Should().Be(patcherNickname);
            patcher.RemoteRepoPath.Should().Be(repoPath);
        }
        finally
        {
            profileDirs.ProfileDirectory.DeleteEntireFolder();
        }
    }
}