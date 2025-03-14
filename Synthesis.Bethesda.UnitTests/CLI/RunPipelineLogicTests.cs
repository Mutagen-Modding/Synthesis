﻿using System.IO.Abstractions;
using Autofac;
using Shouldly;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog.IO;
using Noggog.Testing.AutoFixture;
using Noggog.Testing.Extensions;
using Synthesis.Bethesda.CLI.AddSolutionPatcher;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.CLI.CreateTemplatePatcher;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.UnitTests.CLI;

public class RunPipelineLogicTests
{
    [Theory(Skip = "Need to upgrade to target known published packages"), MutagenModAutoData(FileSystem: TargetFileSystem.Real)]
    public async Task Typical(
        IFileSystem fileSystem,
        string profileName,
        string groupName,
        string patcherNickname,
        SkyrimMod someMod,
        Npc npc)
    {
        using var dataFolder = TempFolder.Factory(fileSystem: fileSystem);
        using var patcherDir = TempFolder.Factory(fileSystem: fileSystem);
        using var outputDir = TempFolder.Factory(fileSystem: fileSystem);
        using var pluginList = new TempFile(fileSystem: fileSystem);
        using var pipelineSettingsFile = new TempFile(fileSystem: fileSystem);
        var name = "TestName";
        var result = await new CreateTemplatePatcherSolutionRunner(fileSystem).Run(new CreateTemplatePatcherCommand()
        {
            PatcherName = name,
            GameCategory = GameCategory.Skyrim,
            ParentDirectory = patcherDir.Dir
        });
        result.ShouldBe(0);
        
        var b = new ContainerBuilder();
        var createProfileCmd = new CreateProfileCommand()
        {
            ProfileName = profileName,
            InitialGroupName = groupName,
            PipelineSettingsPath = pipelineSettingsFile.File,
            GameRelease = GameRelease.SkyrimSE
        };
        b.RegisterModule(new CreateProfileModule(fileSystem, createProfileCmd));
        await b.Build().Resolve<CreateProfileRunner>().RunInternal(createProfileCmd);
        
        b = new ContainerBuilder();
        var solutionDir = Path.Combine(patcherDir.Dir, $"{name}.sln");
        var addSlnCmd = new AddSolutionPatcherCommand()
        {
            ProfileIdentifier = profileName,
            PipelineSettingsPath = pipelineSettingsFile.File,
            SolutionPath = solutionDir,
            ProjectSubpath = Path.Combine(name, $"{name}.csproj"),
            Nickname = patcherNickname,
            GroupName = groupName
        };
        b.RegisterModule(new AddSolutionPatcherModule(fileSystem, addSlnCmd));
        await b.Build().Resolve<AddSolutionPatcherRunner>().Add(addSlnCmd);
        
        await fileSystem.File.WriteAllTextAsync(pluginList.File, $"*{someMod.ModKey.FileName}");
        npc.EditorID = "Before";
        await someMod.BeginWrite.ToPath(Path.Combine(dataFolder.Dir, someMod.ModKey.FileName))
            .WithNoLoadOrder()
            .WithFileSystem(fileSystem)
            .WriteAsync();
        
        await fileSystem.File.WriteAllTextAsync(Path.Combine(patcherDir.Dir, name, "Program.cs"),
            """
            using Mutagen.Bethesda;
            using Mutagen.Bethesda.Synthesis;
            using Mutagen.Bethesda.Skyrim;
            
            namespace TestName
            {
                public class Program
                {
                    public static async Task<int> Main(string[] args)
                    {
                        return await SynthesisPipeline.Instance
                            .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                            .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                            .Run(args);
                    }
            
                    public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
                    {
                        foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
                        {
                            var npcOverride = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                            npcOverride.EditorID = "After";
                        }
                    }
                }
            }
            """);
        
        result = await RunPipelineLogic.Run(new RunPatcherPipelineCommand()
        {
            DataFolderPath = dataFolder.Dir,
            LoadOrderFilePath = pluginList.File,
            ProfileIdentifier = profileName,
            OutputDirectory = outputDir.Dir,
            PipelineSettingsPath = pipelineSettingsFile.File,
        }, fileSystem);
        result.ShouldBe(0);
        var patchFilePath = Path.Combine(outputDir.Dir, ModKey.FromName(groupName, ModType.Plugin).FileName);
        fileSystem.File.Exists(patchFilePath).ShouldBeTrue();
        using var reimport = SkyrimMod.Create(SkyrimRelease.SkyrimSE)
            .FromPath(patchFilePath)
            .WithFileSystem(fileSystem)
            .Construct();
        reimport.Npcs.Select(x => x.EditorID).ShouldEqual("After");
    }
}