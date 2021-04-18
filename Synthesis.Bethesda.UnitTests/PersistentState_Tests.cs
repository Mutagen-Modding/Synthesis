using Mutagen.Bethesda;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Persistence;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using Noggog.Utility;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class PersistentState_Tests
    {
        protected static readonly string AddAwesomeNPCName = "AddAwesomeNPC";

        protected static readonly string AddAnotherNPCName = "AddAnotherNPC";

        protected static readonly string AwesomeNPCEdid = "AwesomeNPC";

        protected static readonly string AnotherNPCEdid = "AnotherNPC";

        protected static ModKey PatchModKey => new("Patch", ModType.Plugin);

        protected static ModPath PatchModPath(TempFolder dataFolder) => new(PatchModKey, Path.Combine(dataFolder.Dir.Path, PatchModKey.ToString()));

        private static string GetStatePath(TempFolder tmpFolder)
        {
            return Path.Combine(tmpFolder.Dir.Path, "StatePath");
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
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                .Run(new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                });

            Assert.True(File.Exists(modPath.Path));
            using var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
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
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var modPath = PatchModPath(dataFolder);
            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                .Run(new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile
                });
            Assert.True(File.Exists(modPath.Path));
            using var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
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
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var statePath = GetStatePath(tmpFolder);
            var modPath = PatchModPath(dataFolder);

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                .Run(new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = null,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile,
                    PersistencePath = statePath,
                    PatcherName = AddAwesomeNPCName
                });

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                .Run(new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = modPath,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile,
                    PersistencePath = statePath,
                    PatcherName = AddAnotherNPCName
                });

            Assert.True(File.Exists(modPath.Path));
            using var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
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
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var statePath = GetStatePath(tmpFolder);
            var modPath = PatchModPath(dataFolder);

            await new SynthesisPipeline()
            .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
            .Run(new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = GameRelease.Oblivion,
                OutputPath = modPath,
                SourcePath = null,
                LoadOrderFilePath = Utility.PathToLoadOrderFile,
                PersistencePath = statePath,
                PatcherName = AddAnotherNPCName
            });

            await new SynthesisPipeline()
                .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                .Run(new RunSynthesisMutagenPatcher()
                {
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = GameRelease.Oblivion,
                    OutputPath = modPath,
                    SourcePath = modPath,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile,
                    PersistencePath = statePath,
                    PatcherName = AddAwesomeNPCName
                });

            Assert.True(File.Exists(modPath.Path));
            using var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
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
            using var tmpFolder = Utility.GetTempFolder();
            var statePath = GetStatePath(tmpFolder);
            TextFileSharedFormKeyAllocator.Initialize(statePath);

            for (int i = 0; i < 2; i++)
            {
                using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
                var modPath = PatchModPath(dataFolder);

                await new SynthesisPipeline()
                    .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                    .Run(new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = dataFolder.Dir.Path,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = null,
                        LoadOrderFilePath = Utility.PathToLoadOrderFile,
                        PersistencePath = statePath,
                        PatcherName = AddAwesomeNPCName
                    });

                await new SynthesisPipeline()
                    .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                    .Run(new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = dataFolder.Dir.Path,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = modPath,
                        LoadOrderFilePath = Utility.PathToLoadOrderFile,
                        PersistencePath = statePath,
                        PatcherName = AddAnotherNPCName
                    });

                if (i == 1)
                {
                    Assert.True(File.Exists(modPath.Path));
                    using var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
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
            using var tmpFolder = Utility.GetTempFolder();
            var statePath = GetStatePath(tmpFolder);
            TextFileSharedFormKeyAllocator.Initialize(statePath);

            {
                using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
                var modPath = PatchModPath(dataFolder);

                await new SynthesisPipeline()
                    .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                    .Run(new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = dataFolder.Dir.Path,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = null,
                        LoadOrderFilePath = Utility.PathToLoadOrderFile,
                        PersistencePath = statePath,
                        PatcherName = AddAwesomeNPCName
                    });

                await new SynthesisPipeline()
                    .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                    .Run(new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = dataFolder.Dir.Path,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = modPath,
                        LoadOrderFilePath = Utility.PathToLoadOrderFile,
                        PersistencePath = statePath,
                        PatcherName = AddAnotherNPCName
                    });
            }

            {
                Assert.True(Directory.Exists(statePath));
                using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
                var modPath = PatchModPath(dataFolder);

                await new SynthesisPipeline()
                    .AddPatch<IOblivionMod, IOblivionModGetter>(AddAnotherNPC)
                    .Run(new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = dataFolder.Dir.Path,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = null,
                        LoadOrderFilePath = Utility.PathToLoadOrderFile,
                        PersistencePath = statePath,
                        PatcherName = AddAnotherNPCName
                    });

                await new SynthesisPipeline()
                    .AddPatch<IOblivionMod, IOblivionModGetter>(AddAwesomeNPC)
                    .Run(new RunSynthesisMutagenPatcher()
                    {
                        DataFolderPath = dataFolder.Dir.Path,
                        GameRelease = GameRelease.Oblivion,
                        OutputPath = modPath,
                        SourcePath = modPath,
                        LoadOrderFilePath = Utility.PathToLoadOrderFile,
                        PersistencePath = statePath,
                        PatcherName = AddAwesomeNPCName
                    });

                Assert.True(File.Exists(modPath.Path));
                using var patch = OblivionMod.CreateFromBinaryOverlay(modPath);
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
}
