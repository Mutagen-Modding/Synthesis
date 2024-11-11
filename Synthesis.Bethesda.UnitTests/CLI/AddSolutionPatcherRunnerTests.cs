using System.IO.Abstractions;
using Autofac;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Synthesis.Bethesda.CLI.AddSolutionPatcher;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class AddSolutionPatcherRunnerTests
{
    [Theory, MutagenModAutoData]
    public async Task Typical(
        IFileSystem fileSystem,
        string slnPatcherName,
        string profileName,
        string patcherNickname,
        string groupName,
        PipelineSettingsPath pipelineSettingsPathProvider,
        PipelineSettingsV2Reader reader,
        DirectoryPath patcherDir,
        DirectoryPath existingSettingsFolder)
    {
        var b = new ContainerBuilder();
        var createProfileCmd = new CreateProfileCommand()
        {
            ProfileName = profileName,
            InitialGroupName = groupName,
            SettingsFolderPath = existingSettingsFolder,
            GameRelease = GameRelease.SkyrimSE
        };
        b.RegisterModule(new CreateProfileModule(fileSystem, createProfileCmd));
        await b.Build().Resolve<CreateProfileRunner>().RunInternal(createProfileCmd);
        var pipelineSettingsPath = Path.Combine(existingSettingsFolder, pipelineSettingsPathProvider.Name);
        var pipeSettings = reader.Read(pipelineSettingsPath);
        
        b = new ContainerBuilder();
        var slnPath = Path.Combine(patcherDir, $"{slnPatcherName}.sln");
        var projSubPath = Path.Combine(slnPatcherName, $"{slnPatcherName}.csproj");
        var addSlnCmd = new AddSolutionPatcherCommand()
        {
            Nickname = patcherNickname,
            GroupName = groupName,
            ProfileIdentifier = profileName,
            SettingsFolderPath = existingSettingsFolder,
            SolutionPath = slnPath,
            ProjectSubpath = projSubPath,
        };
        b.RegisterModule(new AddSolutionPatcherModule(fileSystem, addSlnCmd));
        var cont = b.Build();
        await cont.Resolve<AddSolutionPatcherRunner>().Add(addSlnCmd);
        pipeSettings = reader.Read(pipelineSettingsPath);
        var patcher = pipeSettings.Profiles.First().Groups.First().Patchers.First() as SolutionPatcherSettings;
        patcher!.Nickname.Should().Be(patcherNickname);
        patcher.ProjectSubpath.Should().Be(projSubPath);
        patcher.SolutionPath.Path.Should().Be(slnPath);
    }
}