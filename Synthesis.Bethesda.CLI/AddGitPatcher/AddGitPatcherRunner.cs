﻿using System.IO.Abstractions;
using Autofac;
using Noggog;
using Synthesis.Bethesda.CLI.Common;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.Instantiation;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.AddGitPatcher;

public class AddGitPatcherRunner
{
    private readonly ILifetimeScope _scope;
    private readonly IFileSystem _fileSystem;
    private readonly PipelineSettingsModifier _pipelineSettingsModifier;
    private readonly GitIdAllocator _gitIdAllocator;

    public AddGitPatcherRunner(
        ILifetimeScope scope,
        IFileSystem fileSystem,
        PipelineSettingsModifier pipelineSettingsModifier,
        GitIdAllocator gitIdAllocator)
    {
        _scope = scope;
        _fileSystem = fileSystem;
        _pipelineSettingsModifier = pipelineSettingsModifier;
        _gitIdAllocator = gitIdAllocator;
    }
    
    public async Task Add(AddGitPatcherCommand cmd)
    {
        await _pipelineSettingsModifier.DoModification(cmd.SettingsFolderPath, async (pipelineSettings) =>
        {
            var profile = pipelineSettings.Profiles.First(x => x.Nickname == cmd.ProfileName);
            var group = profile.Groups.First(x => x.Name == cmd.GroupName);
            var patcherIds = pipelineSettings.Profiles
                .SelectMany(x => x.Groups)
                .SelectMany(x => x.Patchers)
                .OfType<GithubPatcherSettings>()
                .Select(x => x.ID)
                .ToHashSet();
            var id = _gitIdAllocator.GetNewId(patcherIds);

            using var subCont = _scope.BeginLifetimeScope((c) =>
            {
                c.RegisterModule(new AddGitPatcherModule(_fileSystem));
                c.RegisterInstance(new GithubPatcherIdentifier(id)).AsImplementedInterfaces();
                c.RegisterInstance(new ProfileIdentifier(profile.ID)).AsImplementedInterfaces();
            });
            
            var driverRepo = subCont.Resolve<IPrepareDriverRespository>()
                .Prepare(GetResponse<string>.Succeed(cmd.GitRepoAddress), CancellationToken.None)
                .EvaluateOrThrow();
            
            var gitSettings = new GithubPatcherSettings()
            {
                ID = id,
                RemoteRepoPath = cmd.GitRepoAddress,
                SelectedProjectSubpath = cmd.SelectedProjectSubpath,
                Nickname = cmd.Nickname ?? string.Empty,
                On = true,
                PatcherVersioning = PatcherVersioningEnum.Branch,
                FollowDefaultBranch = true,
                TargetBranch = driverRepo.MasterBranchName,
                MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                SynthesisVersionType = PatcherNugetVersioningEnum.Profile,
                AutoUpdateToBranchTip = false,
            };
            group.Patchers.Add(gitSettings);
        });
    }
    
    public static async Task<int> Run(AddGitPatcherCommand cmd)
    {
        try
        {
            var b = new ContainerBuilder();
            b.RegisterModule(new AddGitPatcherModule(new FileSystem()));
            var cont = b.Build();
            var adder = cont.Resolve<AddGitPatcherRunner>();
            await adder.Add(cmd);
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine(ex);
            return -1;
        }
        return 0;
    }
}