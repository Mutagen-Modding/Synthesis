using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using Noggog.Utility;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class MutagenSynthesisTests
    {
        private async Task PatchFunction(SynthesisState<IOblivionMod, IOblivionModGetter> state)
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
                        Item = FormKey.Null.AsLink<IItemGetter>()
                    });
            }
        }

        protected static ModKey PatchModKey => new("Patch", ModType.Plugin);
        protected static ModPath PatchModPath(TempFolder dataFolder) => new(PatchModKey, Path.Combine(dataFolder.Dir.Path, PatchModKey.ToString()));

        [Fact]
        public async Task TypicalPatcher_FreshStart()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
#pragma warning disable CS0618 // Type or member is obsolete
            await new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                },
                PatchFunction);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.True(File.Exists(modPath.Path));
            using var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
            Assert.Equal(3, patch.Npcs.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(PatchModKey, 0xD62)].Items.Count);
        }

        [Fact]
        public async Task TypicalPatcher_HasSource()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            var settings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                OutputPath = modPath,
                SourcePath = null,
                LoadOrderFilePath = Utility.PathToLoadOrderFile
            };
#pragma warning disable CS0618 // Type or member is obsolete
            await new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(settings, PatchFunction);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.True(File.Exists(modPath.Path));
            using (var patch = OblivionMod.CreateFromBinaryOverlay(modPath))
            {
                Assert.Equal(3, patch.Npcs.Count);
                Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
                Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
                Assert.Equal(1, patch.Npcs[new FormKey(PatchModKey, 0xD62)].Items.Count);
            }

            // Run a second time, with sourcepath set containing previous patch
            settings.SourcePath = modPath;
#pragma warning disable CS0618 // Type or member is obsolete
            await new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(settings, PatchFunction);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.True(File.Exists(modPath.Path));
            using (var patch = OblivionMod.CreateFromBinaryOverlay(modPath))
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
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                return new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(
                   new RunSynthesisMutagenPatcher()
                   {
                       DataFolderPath = dataFolder.Dir.Path,
                       GameRelease = Mutagen.Bethesda.GameRelease.SkyrimLE,
                       OutputPath = modPath,
                       SourcePath = null,
                       LoadOrderFilePath = Utility.PathToLoadOrderFile
                   },
                   PatchFunction);
#pragma warning restore CS0618 // Type or member is obsolete
            });
        }

        [Fact]
        public void HasSourceModOnLoadOrder()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            using var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState<IOblivionMod, IOblivionModGetter>(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                },
                new PatcherPreferences(),
                Synthesis.Bethesda.Constants.SynthesisModKey);
            Assert.Equal(state.PatchMod.ModKey, state.LoadOrder[^1].ModKey);
        }

        [Fact]
        public void HasSourceModOnLoadOrder_HasSource()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var prevPath = new ModPath(Utility.OverrideModKey, Path.Combine(dataFolder.Dir.Path, Utility.OverrideModKey.FileName));
            var modPath = PatchModPath(dataFolder);
            using var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState<IOblivionMod, IOblivionModGetter>(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = prevPath,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                },
                new PatcherPreferences(),
                Synthesis.Bethesda.Constants.SynthesisModKey);
            Assert.Equal(state.PatchMod.ModKey, state.LoadOrder[^1].ModKey);
        }

        [Fact]
        public void TrimsPostSynthesisFromLoadOrder()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimLE);
            var pluginsPath = Path.Combine(dataFolder.Dir.Path, "Plugins.txt");
            File.WriteAllLines(
                pluginsPath,
                new string[]
                {
                    Utility.TestModKey.FileName,
                    Utility.OverrideModKey.FileName,
                    Constants.SynthesisModKey.FileName,
                    Utility.RandomModKey.FileName
                });
            var prevPath = new ModPath(Utility.OverrideModKey, Path.Combine(dataFolder.Dir.Path, Utility.OverrideModKey.FileName));
            var modPath = PatchModPath(dataFolder);
            using var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState<Mutagen.Bethesda.Skyrim.ISkyrimMod, Mutagen.Bethesda.Skyrim.ISkyrimModGetter>(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.SkyrimLE,
                    OutputPath = modPath,
                    SourcePath = prevPath,
                    LoadOrderFilePath = pluginsPath
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
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimSE);
            var pluginsPath = Path.Combine(dataFolder.Dir.Path, "Plugins.txt");
            File.WriteAllLines(
                pluginsPath,
                new string[]
                {
                    $"*{Utility.TestModKey.FileName}",
                    Utility.OverrideModKey.FileName
                });
            var prevPath = new ModPath(Utility.OverrideModKey, Path.Combine(dataFolder.Dir.Path, Utility.OverrideModKey.FileName));
            var modPath = PatchModPath(dataFolder);
            using var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState<Mutagen.Bethesda.Skyrim.ISkyrimMod, Mutagen.Bethesda.Skyrim.ISkyrimModGetter>(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.SkyrimSE,
                    OutputPath = modPath,
                    SourcePath = prevPath,
                    LoadOrderFilePath = pluginsPath
                },
                new PatcherPreferences(),
                Synthesis.Bethesda.Constants.SynthesisModKey);
            state.LoadOrder.PriorityOrder.Should().HaveCount(2);
            state.RawLoadOrder.Should().HaveCount(3);
            state.RawLoadOrder[0].Should().Be(new LoadOrderListing(Utility.TestModKey, true));
            state.RawLoadOrder[1].Should().Be(new LoadOrderListing(Utility.OverrideModKey, false));
            state.RawLoadOrder[2].Should().Be(new LoadOrderListing(Utility.SynthesisModKey, true));
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
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var pluginsPath = Path.Combine(dataFolder.Dir.Path, "Plugins.txt");
            File.WriteAllLines(
                pluginsPath,
                new string[]
                {
                    Utility.TestModKey.FileName,
                    Utility.OverrideModKey.FileName
                });

            var mod = new OblivionMod(Utility.TestModKey);
            mod.WriteToBinary(Path.Combine(dataFolder.Dir.Path, Utility.TestModKey.FileName));
            var mod2 = new OblivionMod(Utility.OverrideModKey);
            mod2.Npcs.Add(new Npc(mod.GetNextFormKey()));
            mod2.WriteToBinary(Path.Combine(dataFolder.Dir.Path, Utility.OverrideModKey.FileName));

            var list = new ExtendedList<LoadOrderListing>()
            {
                new LoadOrderListing(Utility.TestModKey, false),
                new LoadOrderListing(Utility.OverrideModKey, true),
            };
            Mutagen.Bethesda.Synthesis.Internal.Utility.AddImplicitMasters(
                new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = GameRelease.Oblivion
                },
                list);

            list.Should().HaveCount(2);
            list[0].Should().Be(new LoadOrderListing(Utility.TestModKey, true));
            list[1].Should().Be(new LoadOrderListing(Utility.OverrideModKey, true));
        }

        [Fact]
        public void NoPatch()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            var settings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                OutputPath = string.Empty,
                SourcePath = null,
                LoadOrderFilePath = Utility.PathToLoadOrderFile
            };
#pragma warning disable CS0618 // Type or member is obsolete
            new SynthesisPipeline().Patch<IOblivionMod, IOblivionModGetter>(
                settings,
                (state) => { },
                new UserPreferences()
                {
                    NoPatch = true
                });
#pragma warning restore CS0618 // Type or member is obsolete
            File.Exists(modPath.Path).Should().BeFalse();
        }
    }
}
