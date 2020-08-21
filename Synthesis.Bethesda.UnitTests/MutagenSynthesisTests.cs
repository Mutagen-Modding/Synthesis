using Alphaleonis.Win32.Filesystem;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class MutagenSynthesisTests
    {
        private void PatchFunction(SynthesisState<IOblivionMod, IOblivionModGetter> state)
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
                        Item = FormKey.Null
                    });
            }
        }

        protected ModKey PatchModKey => new ModKey("Patch", ModType.Plugin);
        protected ModPath PatchModPath(TempFolder dataFolder) => new ModPath(PatchModKey, Path.Combine(dataFolder.Dir.Path, PatchModKey.ToString()));

        [Fact]
        public void TypicalPatcher_FreshStart()
        {
            using var dataFolder = Utility.SetupDataFolder(GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            SynthesisPipeline.Instance.Patch<IOblivionMod, IOblivionModGetter>(
                new RunSynthesisPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                },
                PatchFunction);
            Assert.True(File.Exists(modPath.Path));
            var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
            Assert.Equal(3, patch.Npcs.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD62)].Items.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(Utility.TestModKey, 0xD63)].Items.Count);
            Assert.Equal(1, patch.Npcs[new FormKey(PatchModKey, 0xD62)].Items.Count);
        }

        [Fact]
        public void TypicalPatcher_HasSource()
        {
            using var dataFolder = Utility.SetupDataFolder(GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            var settings = new RunSynthesisPatcher()
            {
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                OutputPath = modPath,
                SourcePath = null,
                LoadOrderFilePath = Utility.PathToLoadOrderFile
            };
            SynthesisPipeline.Instance.Patch<IOblivionMod, IOblivionModGetter>(settings, PatchFunction);
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
            SynthesisPipeline.Instance.Patch<IOblivionMod, IOblivionModGetter>(settings, PatchFunction);
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
        public void MisalignedGameTypes()
        {
            using var dataFolder = Utility.SetupDataFolder(GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            Assert.Throws<ArgumentException>(() =>
            {
                SynthesisPipeline.Instance.Patch<IOblivionMod, IOblivionModGetter>(
                   new RunSynthesisPatcher()
                   {
                       DataFolderPath = dataFolder.Dir.Path,
                       GameRelease = Mutagen.Bethesda.GameRelease.SkyrimLE,
                       OutputPath = modPath,
                       SourcePath = null,
                       LoadOrderFilePath = Utility.PathToLoadOrderFile
                   },
                   PatchFunction);
            });
        }

        [Fact]
        public void HasSourceModOnLoadOrder()
        {
            using var dataFolder = Utility.SetupDataFolder(GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState<IOblivionMod, IOblivionModGetter>(
                new RunSynthesisPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                },
                new UserPreferences());
            Assert.Equal(state.PatchMod.ModKey, state.LoadOrder.Last().Key);
        }

        [Fact]
        public void HasSourceModOnLoadOrder_HasSource()
        {
            using var dataFolder = Utility.SetupDataFolder(GameRelease.Oblivion);
            var prevPath = new ModPath(Utility.OverrideModKey, Path.Combine(dataFolder.Dir.Path, Utility.OverrideModKey.FileName));
            var modPath = PatchModPath(dataFolder);
            var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState<IOblivionMod, IOblivionModGetter>(
                new RunSynthesisPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = Mutagen.Bethesda.GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = prevPath,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                },
                new UserPreferences());
            Assert.Equal(state.PatchMod.ModKey, state.LoadOrder.Last().Key);
        }

        [Fact]
        public void EmptyArgs_NoRun()
        {
            SynthesisPipeline.Instance.Patch<IOblivionMod, IOblivionModGetter>(
                new string[0],
                PatchFunction);
        }
    }
}
