using Shouldly;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Masters.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Synthesis.States;
using Noggog.Testing.Extensions;
using Synthesis.Bethesda.UnitTests.Common;
using Path = System.IO.Path;

namespace Synthesis.Bethesda.UnitTests;

public class MutagenSynthesisTests
{
#pragma warning disable CS0618 // Type or member is obsolete
    private async Task PatchFunction(SynthesisState<IOblivionMod, IOblivionModGetter> state)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        // Add a new NPC
        state.PatchMod.Npcs.AddNew();

        //Add a null item entry to all NPCs
        foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
        {
            var patchNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
            patchNpc.Items.Add(
                new ItemEntry()
                {
                    Count = 1,
                    Item = FormKey.Null.ToLink<IItemGetter>()
                });
        }
    }

    protected static ModKey PatchModKey => new("Patch", ModType.Plugin);
    protected static ModPath PatchModPath(DirectoryPath dataFolder) => new(PatchModKey, Path.Combine(dataFolder.Path, PatchModKey.ToString()));

    [Fact]
    public async Task TypicalPatcher_FreshStart()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var modPath = PatchModPath(env.DataFolder);
#pragma warning disable CS0618 // Type or member is obsolete
        await new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(
            new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = env.DataFolder,
                GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                OutputPath = modPath,
                SourcePath = null,
                LoadOrderFilePath = env.PluginPath
            },
            PatchFunction,
            fileSystem: env.FileSystem);
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.True(env.FileSystem.File.Exists(modPath.Path));
        using var patch = OblivionMod.Create(OblivionRelease.Oblivion)
            .FromPath(modPath)
            .WithFileSystem(env.FileSystem)
            .Construct();
        Assert.Equal(3, patch.Npcs.Count);
        Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
        Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
        Assert.Equal(1, patch.Npcs[new FormKey(PatchModKey, 0xD62)].Items.Count);
    }

    [Fact]
    public async Task TypicalPatcher_HasSource()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var modPath = PatchModPath(env.DataFolder);
        var settings = new RunSynthesisMutagenPatcher()
        {
            DataFolderPath = env.DataFolder,
            GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
            OutputPath = modPath,
            SourcePath = null,
            LoadOrderFilePath = env.PluginPath
        };
#pragma warning disable CS0618 // Type or member is obsolete
        await new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(settings, PatchFunction, fileSystem: env.FileSystem);
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.True(env.FileSystem.File.Exists(modPath.Path));
        using (var patch = OblivionMod.Create(OblivionRelease.Oblivion)
                   .FromPath(modPath)
                   .WithFileSystem(env.FileSystem)
                   .Construct())
        {
            Assert.Equal(3, patch.Npcs.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(PatchModKey, 0xD62)].Items.Count);
        }

        // Run a second time, with sourcepath set containing previous patch
        settings.SourcePath = modPath;
#pragma warning disable CS0618 // Type or member is obsolete
        await new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(settings, PatchFunction, fileSystem: env.FileSystem);
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.True(env.FileSystem.File.Exists(modPath.Path));
        using (var patch = OblivionMod.Create(OblivionRelease.Oblivion)
                   .FromPath(modPath)
                   .WithFileSystem(env.FileSystem)
                   .Construct())
        {
            Assert.Equal(4, patch.Npcs.Count);
            Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
            Assert.Equal(2, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
            Assert.Equal(2, patch.Npcs[new FormKey(PatchModKey, 0xD62)].Items.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(PatchModKey, 0xD63)].Items.Count);
        }
    }

    [Fact]
    public async Task MisalignedGameTypes()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var modPath = PatchModPath(env.DataFolder);
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = env.DataFolder,
                    GameRelease = Mutagen.Bethesda.GameRelease.SkyrimLE,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = env.PluginPath
                },
                PatchFunction,
                fileSystem: env.FileSystem);
#pragma warning restore CS0618 // Type or member is obsolete
        });
    }

    [Fact]
    public void HasSourceModOnLoadOrder()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var modPath = PatchModPath(env.DataFolder);
        var stateFactory = env.GetStateFactory();
        using var state = stateFactory.ToState<IOblivionMod, IOblivionModGetter>(
            new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = env.DataFolder,
                GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                OutputPath = modPath,
                SourcePath = null,
                LoadOrderFilePath = env.PluginPath
            },
            new PatcherPreferences(),
            Synthesis.Bethesda.Constants.SynthesisModKey);
        Assert.Equal(state.PatchMod.ModKey, state.LoadOrder[^1].ModKey);
    }

    [Fact]
    public void HasSourceModOnLoadOrder_HasSource()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var prevPath = new ModPath(Utility.OverrideModKey, Path.Combine(env.DataFolder, Utility.OverrideModKey.FileName));
        var modPath = PatchModPath(env.DataFolder);
        var stateFactory = env.GetStateFactory();
        using var state = stateFactory.ToState<IOblivionMod, IOblivionModGetter>(
            new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = env.DataFolder,
                GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                OutputPath = modPath,
                SourcePath = prevPath,
                LoadOrderFilePath = env.PluginPath
            },
            new PatcherPreferences(),
            Synthesis.Bethesda.Constants.SynthesisModKey);
        Assert.Equal(state.PatchMod.ModKey, state.LoadOrder[^1].ModKey);
    }

    [Fact]
    public void TrimsPostSynthesisFromLoadOrder()
    {
        var env = Utility.SetupEnvironment(GameRelease.SkyrimLE);
        env.FileSystem.File.WriteAllLines(
            env.PluginPath,
            new string[]
            {
                Utility.TestModKey.FileName,
                Utility.OverrideModKey.FileName,
                Constants.SynthesisModKey.FileName,
                Utility.RandomModKey.FileName
            });
        var prevPath = new ModPath(Utility.OverrideModKey, Path.Combine(env.DataFolder, Utility.OverrideModKey.FileName));
        var modPath = PatchModPath(env.DataFolder);
        var stateFactory = env.GetStateFactory();
        using var state = stateFactory.ToState<Mutagen.Bethesda.Skyrim.ISkyrimMod, Mutagen.Bethesda.Skyrim.ISkyrimModGetter>(
            new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = env.DataFolder,
                GameRelease = Mutagen.Bethesda.GameRelease.SkyrimLE,
                OutputPath = modPath,
                SourcePath = prevPath,
                LoadOrderFilePath = env.PluginPath
            },
            new PatcherPreferences(),
            Synthesis.Bethesda.Constants.SynthesisModKey);
        Assert.Equal(3, state.LoadOrder.Count);
        Assert.Equal(Utility.TestModKey, state.LoadOrder[0].ModKey);
        Assert.Equal(Utility.OverrideModKey, state.LoadOrder[1].ModKey);
        Assert.Equal(Constants.SynthesisModKey, state.LoadOrder[2].ModKey);
    }

    [Fact]
    public void DisabledModsInLoadOrder()
    {
        var env = Utility.SetupEnvironment(GameRelease.SkyrimSE);
        env.FileSystem.File.WriteAllLines(
            env.PluginPath,
            new string[]
            {
                $"*{Utility.TestModKey.FileName}",
                Utility.OverrideModKey.FileName
            });
        var prevPath = new ModPath(Utility.OverrideModKey, Path.Combine(env.DataFolder, Utility.OverrideModKey.FileName));
        var modPath = PatchModPath(env.DataFolder);
        var stateFactory = env.GetStateFactory();
        using var state = stateFactory.ToState<Mutagen.Bethesda.Skyrim.ISkyrimMod, Mutagen.Bethesda.Skyrim.ISkyrimModGetter>(
            new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = env.DataFolder,
                GameRelease = Mutagen.Bethesda.GameRelease.SkyrimSE,
                OutputPath = modPath,
                SourcePath = prevPath,
                LoadOrderFilePath = env.PluginPath
            },
            new PatcherPreferences(),
            Synthesis.Bethesda.Constants.SynthesisModKey);
        state.LoadOrder.PriorityOrder.ShouldHaveCount(2);
        state.RawLoadOrder.ShouldHaveCount(3);
        state.RawLoadOrder[0].ShouldBe(new LoadOrderListing(Utility.TestModKey, true));
        state.RawLoadOrder[1].ShouldBe(new LoadOrderListing(Utility.OverrideModKey, false));
        state.RawLoadOrder[2].ShouldBe(new LoadOrderListing(Utility.SynthesisModKey, true));
    }

    [Fact]
    public async Task EmptyArgs_NoRun()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        await new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(
            Array.Empty<string>(),
            PatchFunction);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void AddImplicitMasters()
    {
        using var tmpFolder = Utility.GetTempFolder(nameof(MutagenSynthesisTests));
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        env.FileSystem.File.WriteAllLines(
            env.PluginPath,
            new string[]
            {
                Utility.TestModKey.FileName,
                Utility.OverrideModKey.FileName
            });

        var mod = new OblivionMod(Utility.TestModKey, OblivionRelease.Oblivion);
        mod.BeginWrite
            .ToPath(Path.Combine(env.DataFolder, Utility.TestModKey.FileName))
            .WithNoLoadOrder()
            .WithFileSystem(env.FileSystem)
            .Write();
        var mod2 = new OblivionMod(Utility.OverrideModKey, OblivionRelease.Oblivion);
        mod2.Npcs.Add(new Npc(mod.GetNextFormKey(), OblivionRelease.Oblivion));
        mod2.BeginWrite
            .ToPath(Path.Combine(env.DataFolder, Utility.OverrideModKey.FileName))
            .WithNoLoadOrder()
            .WithFileSystem(env.FileSystem)
            .Write();

        var list = new ExtendedList<ILoadOrderListingGetter>()
        {
            new LoadOrderListing(Utility.TestModKey, false),
            new LoadOrderListing(Utility.OverrideModKey, true),
        };
        new EnableImplicitMasters(
                new FindImplicitlyIncludedMods(
                    env.FileSystem,
                    new DataDirectoryInjection(env.DataFolder),
                    new MasterReferenceReaderFactory(
                        env.FileSystem,
                        new GameReleaseInjection(env.Release))))
            .Add(list);

        list.ShouldHaveCount(2);
        list[0].ShouldBe(new LoadOrderListing(Utility.TestModKey, true));
        list[1].ShouldBe(new LoadOrderListing(Utility.OverrideModKey, true));
    }

    [Fact]
    public void NoPatch()
    {
        var env = Utility.SetupEnvironment(GameRelease.Oblivion);
        var modPath = PatchModPath(env.DataFolder);
        var settings = new RunSynthesisMutagenPatcher()
        {
            DataFolderPath = env.DataFolder,
            GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
            OutputPath = string.Empty,
            SourcePath = null,
            LoadOrderFilePath = env.PluginPath
        };
#pragma warning disable CS0618 // Type or member is obsolete
        new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(
            settings,
            (state) => { },
            new UserPreferences()
            {
                NoPatch = true
            },
            env.FileSystem);
#pragma warning restore CS0618 // Type or member is obsolete
        env.FileSystem.File.Exists(modPath.Path).ShouldBeFalse();
    }
}