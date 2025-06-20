using Mutagen.Bethesda;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using Synthesis.Bethesda.UnitTests.Common;
using Path = System.IO.Path;

namespace Synthesis.Bethesda.UnitTests;

public class PersistentState_Tests : IClassFixture<LoquiUse>
{
    protected static readonly string AddAwesomeNPCName = "AddAwesomeNPC";

    protected static readonly string AddAnotherNPCName = "AddAnotherNPC";

    protected static readonly string AwesomeNPCEdid = "AwesomeNPC";

    protected static readonly string AnotherNPCEdid = "AnotherNPC";

    protected static ModKey PatchModKey => new("Patch", ModType.Plugin);

    protected static ModPath PatchModPath(DirectoryPath dataFolder) => new(PatchModKey, Path.Combine(dataFolder.Path, PatchModKey.ToString()));

    private static string GetStatePath(DirectoryPath tmpFolder)
    {
        return Path.Combine(tmpFolder.Path, "StatePath");
    }

    private static void AddItemToAllNPCs(IPatcherState<IOblivionMod, IOblivionModGetter> state)
    {
        var item = new ItemEntry()
        {
            Count = 1,
        };
        item.Item.SetTo(FormLink<IItemGetter>.Null);

        foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            state.PatchMod.Npcs.GetOrAddAsOverride(npc).Items.Add(item);
    }

    private async Task AddAwesomeNPC(IPatcherState<IOblivionMod, IOblivionModGetter> state)
    {
        // Add a new awesome NPC
        state.PatchMod.Npcs.AddNew(AwesomeNPCEdid);
        AddItemToAllNPCs(state);
    }

    private async Task AddAnotherNPC(IPatcherState<IOblivionMod, IOblivionModGetter> state)
    {
        // Add another new NPC
        state.PatchMod.Npcs.AddNew(AnotherNPCEdid);
        AddItemToAllNPCs(state);
    }

    #region FreshStart

    [Fact]
    public async Task FreshStart_AwesomeNPC()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var modPath = PatchModPath(env.DataFolder);

        await new SynthesisPipeline()
            .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
            .Run(
                new RunSynthesisMutagenPatcher() 
                {
                    DataFolderPath = env.DataFolder,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = env.PluginPath
                }, 
                fileSystem: env.FileSystem);

        Assert.True(env.FileSystem.File.Exists(modPath.Path));
        using var patch = OblivionMod.Create(OblivionRelease.Oblivion)
            .FromPath(modPath)
            .WithFileSystem(env.FileSystem)
            .Construct();
        Assert.Equal(3, patch.Npcs.Count);
        Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
        Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
        var awesomeNPC = patch.Npcs[new FormKey(PatchModKey, 0xD62)];
        Assert.Equal(1, awesomeNPC.Items.Count);
        Assert.Equal(AwesomeNPCEdid, awesomeNPC.EditorID);
    }

    [Fact]
    public async Task FreshStart_AnotherNPC()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var modPath = PatchModPath(env.DataFolder);
        await new SynthesisPipeline()
            .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
            .Run(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = env.DataFolder,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = env.PluginPath
                },
                fileSystem: env.FileSystem);
        Assert.True(env.FileSystem.File.Exists(modPath.Path));
        using var patch = OblivionMod.Create(OblivionRelease.Oblivion)
            .FromPath(modPath)
            .WithFileSystem(env.FileSystem)
            .Construct();
        Assert.Equal(3, patch.Npcs.Count);
        Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
        Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
        var anotherNPC = patch.Npcs[new FormKey(PatchModKey, 0xD62)];
        Assert.Equal(1, anotherNPC.Items.Count);
        Assert.Equal(AnotherNPCEdid, anotherNPC.EditorID);
    }

    [Fact]
    public async Task FreshStart_AwesomeNPCAndAnotherNPC()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var statePath = GetStatePath(env.BaseFolder);
        var modPath = PatchModPath(env.DataFolder);

        await new SynthesisPipeline()
            .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
            .Run(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = env.DataFolder,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = env.PluginPath,
                    PersistencePath = statePath,
                    PatcherName = AddAwesomeNPCName
                },
                fileSystem: env.FileSystem);

        await new SynthesisPipeline()
            .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
            .Run(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = env.DataFolder,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = modPath,
                    LoadOrderFilePath = env.PluginPath,
                    PersistencePath = statePath,
                    PatcherName = AddAnotherNPCName
                },
                fileSystem: env.FileSystem);

        Assert.True(env.FileSystem.File.Exists(modPath.Path));
        using var patch = OblivionMod.Create(OblivionRelease.Oblivion)
            .FromPath(modPath)
            .WithFileSystem(env.FileSystem)
            .Construct();
        Assert.Equal(4, patch.Npcs.Count);
        Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
        Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
        var awesomeNPC = patch.Npcs[new FormKey(PatchModKey, 0xD62)];
        Assert.Equal(2, awesomeNPC.Items.Count);
        Assert.Equal(AwesomeNPCEdid, awesomeNPC.EditorID);
        var anotherNPC = patch.Npcs[new FormKey(PatchModKey, 0xD63)];
        Assert.Equal(1, anotherNPC.Items.Count);
        Assert.Equal(AnotherNPCEdid, anotherNPC.EditorID);
    }

    [Fact]
    public async Task FreshStart_AnotherNPCAndAwesomeNPC()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var statePath = GetStatePath(env.BaseFolder);
        var modPath = PatchModPath(env.DataFolder);

        await new SynthesisPipeline()
            .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
            .Run(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = env.DataFolder,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = env.PluginPath,
                    PersistencePath = statePath,
                    PatcherName = AddAnotherNPCName
                },
                env.FileSystem);

        await new SynthesisPipeline()
            .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
            .Run(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = env.DataFolder,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = modPath,
                    LoadOrderFilePath = env.PluginPath,
                    PersistencePath = statePath,
                    PatcherName = AddAwesomeNPCName
                },
                env.FileSystem);

        Assert.True(env.FileSystem.File.Exists(modPath.Path));
        using var patch = OblivionMod.Create(OblivionRelease.Oblivion)
            .FromPath(modPath)
            .WithFileSystem(env.FileSystem)
            .Construct();
        Assert.Equal(4, patch.Npcs.Count);
        Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
        Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
        var anotherNPC = patch.Npcs[new FormKey(PatchModKey, 0xD62)];
        Assert.Equal(2, anotherNPC.Items.Count);
        Assert.Equal(AnotherNPCEdid, anotherNPC.EditorID);
        var awesomeNPC = patch.Npcs[new FormKey(PatchModKey, 0xD63)];
        Assert.Equal(1, awesomeNPC.Items.Count);
        Assert.Equal(AwesomeNPCEdid, awesomeNPC.EditorID);
    }

    #endregion FreshStart

    #region SecondRun
    [Fact]
    public async Task SecondRun_AwesomeNPCAndAnotherNPC()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var statePath = GetStatePath(env.BaseFolder);
        TextFileSharedFormKeyAllocator.Initialize(statePath);

        for (int i = 0; i < 2; i++)
        {
            var modPath = PatchModPath(env.DataFolder);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                .Run(
                    new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = env.DataFolder,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = null,
                        LoadOrderFilePath = env.PluginPath,
                        PersistencePath = statePath,
                        PatcherName = AddAwesomeNPCName
                    },
                    fileSystem : env.FileSystem);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                .Run(
                    new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = env.DataFolder,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = modPath,
                        LoadOrderFilePath = env.PluginPath,
                        PersistencePath = statePath,
                        PatcherName = AddAnotherNPCName
                    },
                    fileSystem: env.FileSystem);

            if (i == 1)
            {
                Assert.True(env.FileSystem.File.Exists(modPath.Path));
                using var patch = OblivionMod.Create(OblivionRelease.Oblivion)
                    .FromPath(modPath)
                    .WithFileSystem(env.FileSystem)
                    .Construct();
                Assert.Equal(4, patch.Npcs.Count);
                Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
                Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
                var awesomeNPC = patch.Npcs[new FormKey(PatchModKey, 0xD62)];
                Assert.Equal(2, awesomeNPC.Items.Count);
                Assert.Equal(AwesomeNPCEdid, awesomeNPC.EditorID);
                var anotherNPC = patch.Npcs[new FormKey(PatchModKey, 0xD63)];
                Assert.Equal(1, anotherNPC.Items.Count);
                Assert.Equal(AnotherNPCEdid, anotherNPC.EditorID);
            }
        }
    }

    [Fact]
    public async Task SecondRun_AwesomeNPCAndAnotherNPCSwapped()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var statePath = GetStatePath(env.BaseFolder);
        TextFileSharedFormKeyAllocator.Initialize(statePath);

        {
            var modPath = PatchModPath(env.DataFolder);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                .Run(
                    new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = env.DataFolder,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = null,
                        LoadOrderFilePath = env.PluginPath,
                        PersistencePath = statePath,
                        PatcherName = AddAwesomeNPCName
                    },
                    fileSystem: env.FileSystem);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                .Run(
                    new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = env.DataFolder,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = modPath,
                        LoadOrderFilePath = env.PluginPath,
                        PersistencePath = statePath,
                        PatcherName = AddAnotherNPCName
                    },
                    fileSystem: env.FileSystem);
        }

        {
            Assert.True(env.FileSystem.Directory.Exists(statePath));
            var modPath = PatchModPath(env.DataFolder);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                .Run(
                    new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = env.DataFolder,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = null,
                        LoadOrderFilePath = env.PluginPath,
                        PersistencePath = statePath,
                        PatcherName = AddAnotherNPCName
                    },
                    fileSystem: env.FileSystem);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                .Run(
                    new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = env.DataFolder,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = modPath,
                        LoadOrderFilePath = env.PluginPath,
                        PersistencePath = statePath,
                        PatcherName = AddAwesomeNPCName
                    },
                    fileSystem: env.FileSystem);

            Assert.True(env.FileSystem.File.Exists(modPath.Path));
            using var patch = OblivionMod.Create(OblivionRelease.Oblivion)
                .FromPath(modPath)
                .WithFileSystem(env.FileSystem)
                .Construct();
            Assert.Equal(4, patch.Npcs.Count);
            Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
            Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
            var awesomeNPC = patch.Npcs[new FormKey(PatchModKey, 0xD62)];
            Assert.Equal(1, awesomeNPC.Items.Count);
            Assert.Equal(AwesomeNPCEdid, awesomeNPC.EditorID);
            var anotherNPC = patch.Npcs[new FormKey(PatchModKey, 0xD63)];
            Assert.Equal(2, anotherNPC.Items.Count);
            Assert.Equal(AnotherNPCEdid, anotherNPC.EditorID);
        }
    }
    #endregion


}