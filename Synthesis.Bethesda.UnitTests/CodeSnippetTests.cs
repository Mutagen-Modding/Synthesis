using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Oblivion;

namespace Synthesis.Bethesda.UnitTests
{
    public class CodeSnippetTests
    {
        [Fact]
        public async Task CompileBasic()
        {
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = @"// Let's do work! 
                    int wer = 23; 
                    wer++;",
                Nickname = "UnitTests",
            };
            var snippet = new CodeSnippetPatcherRun(settings);
            var result = snippet.Compile(GameRelease.SkyrimSE, CancellationToken.None, out var _);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task CompileWithMutagenCore()
        {
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = $"var modPath = {nameof(ModPath)}.{nameof(ModPath.Empty)}; modPath.Equals({nameof(ModPath)}.{nameof(ModPath.Empty)});",
                Nickname = "UnitTests",
            };
            var snippet = new CodeSnippetPatcherRun(settings);
            var result = snippet.Compile(GameRelease.SkyrimSE, CancellationToken.None, out var _);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task CompileWithSpecificGames()
        {
            foreach (var game in EnumExt.GetValues<GameCategory>())
            {
                var settings = new CodeSnippetPatcherSettings()
                {
                    On = true,
                    Code = $"var id = {game}Mod.DefaultInitialNextFormID; id++;",
                    Nickname = "UnitTests",
                };
                var snippet = new CodeSnippetPatcherRun(settings);
                var result = snippet.Compile(GameRelease.SkyrimSE, CancellationToken.None, out var _);
                Assert.True(result.Success);
            }
        }

        [Fact]
        public async Task BasicRun()
        {
            using var dataFolder = Utility.SetupDataFolder();
            using var tmpFolder = Utility.GetTempFolder();
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = @"// Let's do work! 
                    int wer = 23; 
                    wer++;",
                Nickname = "UnitTests",
            };
            var outputFile = Utility.TypicalOutputFile(tmpFolder);
            var snippet = new CodeSnippetPatcherRun(settings);
            await snippet.Prep(GameRelease.Oblivion);
            await snippet.Run(new RunSynthesisPatcher()
            {
                OutputPath = ModPath.FromPath(outputFile),
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = GameRelease.Oblivion,
                LoadOrderFilePath = Utility.PathToLoadOrderFile,
                SourcePath = null
            });
        }

        [Fact]
        public async Task CreatesOutput()
        {
            using var dataFolder = Utility.SetupDataFolder();
            using var tmpFolder = Utility.GetTempFolder();
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = @"// Let's do work! 
                    int wer = 23; 
                    wer++;",
                Nickname = "UnitTests",
            };
            var outputFile = Utility.TypicalOutputFile(tmpFolder);
            var snippet = new CodeSnippetPatcherRun(settings);
            await snippet.Prep(GameRelease.Oblivion);
            await snippet.Run(new RunSynthesisPatcher()
            {
                OutputPath = ModPath.FromPath(outputFile),
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = GameRelease.Oblivion,
                LoadOrderFilePath = Utility.PathToLoadOrderFile,
                SourcePath = null
            });
            Assert.True(File.Exists(outputFile));
        }

        [Fact]
        public async Task RunTwice()
        {
            using var dataFolder = Utility.SetupDataFolder();
            using var tmpFolder = Utility.GetTempFolder();
            var outputFile = Utility.TypicalOutputFile(tmpFolder);
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = @"state.PatchMod.Npcs.AddNew();",
                Nickname = "UnitTests",
            };
            for (int i = 0; i < 2; i++)
            {
                var snippet = new CodeSnippetPatcherRun(settings);
                await snippet.Prep(GameRelease.Oblivion);
                await snippet.Run(new RunSynthesisPatcher()
                {
                    OutputPath = ModPath.FromPath(outputFile),
                    DataFolderPath = dataFolder.Dir.Path,
                    GameRelease = GameRelease.Oblivion,
                    LoadOrderFilePath = Utility.PathToLoadOrderFile,
                    SourcePath = i == 1 ? outputFile : null
                });
            }
            var mod = OblivionMod.CreateFromBinaryOverlay(outputFile);
            Assert.Equal(2, mod.Npcs.Count);
        }

        [Fact]
        public void ConstructStateFactory()
        {
            using var dataFolder = Utility.SetupDataFolder();
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var settings = new RunSynthesisPatcher()
            {
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = GameRelease.Oblivion,
                LoadOrderFilePath = Utility.PathToLoadOrderFile,
                OutputPath = output,
                SourcePath = null
            };
            var factory = CodeSnippetPatcherRun.ConstructStateFactory(GameRelease.Oblivion);
            var stateObj = factory(settings);
            Assert.NotNull(stateObj);
            SynthesisState<IOblivionMod, IOblivionModGetter>? state = stateObj as SynthesisState<IOblivionMod, IOblivionModGetter>;
            Assert.NotNull(state);
        }
    }
}
