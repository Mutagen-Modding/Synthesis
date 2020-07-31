using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Synthesis.Core.Patchers;
using Mutagen.Bethesda.Synthesis.Core.Settings;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mutagen.Bethesda.Synthesis.UnitTests
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
            var snippet = new CodeSnippetPatcher(settings);
            var result = snippet.Compile(CancellationToken.None, out var _);
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
            var snippet = new CodeSnippetPatcher(settings);
            var result = snippet.Compile(CancellationToken.None, out var _);
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
                    Code = $"var id = {game}Mod.{nameof(OblivionMod.DefaultInitialNextFormID)}; id++;",
                    Nickname = "UnitTests",
                };
                var snippet = new CodeSnippetPatcher(settings);
                var result = snippet.Compile(CancellationToken.None, out var _);
                Assert.True(result.Success);
            }
        }

        [Fact]
        public async Task BasicRun()
        {
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = @"// Let's do work! 
                    int wer = 23; 
                    wer++;",
                Nickname = "UnitTests",
            };
            using var file = new TempFile(extraDirectoryPaths: Utility.TempFolderPath);
            var snippet = new CodeSnippetPatcher(settings);
            await snippet.Prep();
            await snippet.Run(null, new ModPath(Utility.ModKey, file.File.Path));
        }

        [Fact]
        public async Task CreatesOutput()
        {
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = "File.WriteAllText(outputPath, \"Hello\");",
                Nickname = "UnitTests",
            };
            using var file = new TempFile(extraDirectoryPaths: Utility.TempFolderPath);
            Assert.False(file.File.Exists);
            var snippet = new CodeSnippetPatcher(settings);
            await snippet.Prep();
            await snippet.Run(null, new ModPath(Utility.ModKey, file.File.Path));
            Assert.True(file.File.Exists);
        }

        [Fact]
        public async Task RunTwice()
        {
            var settings = new CodeSnippetPatcherSettings()
            {
                On = true,
                Code = "File.WriteAllText(outputPath, \"Hello\");",
                Nickname = "UnitTests",
            };
            for (int i = 0; i < 2; i++)
            {
                using var file = new TempFile(extraDirectoryPaths: Utility.TempFolderPath);
                Assert.False(file.File.Exists);
                var snippet = new CodeSnippetPatcher(settings);
                await snippet.Prep();
                await snippet.Run(null, new ModPath(Utility.ModKey, file.File.Path));
                Assert.True(file.File.Exists);
            }
        }
    }
}
