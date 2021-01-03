using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class ToStateTests
    {
        [Fact]
        public void LoadOrderTrim()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimLE);
            var output = Utility.TypicalOutputFile(tmpFolder);
            var pluginPath = Path.Combine(tmpFolder.Dir.Path, "Plugins.txt");
            File.WriteAllLines(pluginPath,
                new string[]
                {
                    $"*{Utility.TestFileName}",
                    $"*{Utility.OverrideFileName}",
                    $"*{output.ModKey.FileName}",
                    $"*{Utility.OtherFileName}",
                });
            var settings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = GameRelease.SkyrimLE,
                LoadOrderFilePath = Utility.PathToLoadOrderFile,
                OutputPath = output,
                SourcePath = null
            };
            using var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState(
                GameCategory.Skyrim,
                settings,
                new PatcherPreferences(),
                Synthesis.Bethesda.Constants.SynthesisModKey);
            state.RawLoadOrder.Should().HaveCount(3);
            state.RawLoadOrder.Select(l => l.ModKey).Should().Equal(new ModKey[]
            {
                Utility.TestFileName,
                Utility.OverrideModKey,
                output.ModKey,
            });
        }

        [Fact]
        public void NonSynthesisTarget()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimLE);
            var output = ModPath.FromPath(Path.Combine(dataFolder.Dir.Path, Utility.OtherFileName));
            var pluginPath = Path.Combine(tmpFolder.Dir.Path, "Plugins.txt");
            File.WriteAllLines(pluginPath,
                new string[]
                {
                    $"*{Utility.TestFileName}",
                    $"*{Utility.OverrideFileName}",
                    $"*{output.ModKey.FileName}",
                    $"*{Utility.OtherFileName}",
                    $"*{Utility.SynthesisModKey}",
                });
            var settings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataFolder.Dir.Path,
                GameRelease = GameRelease.SkyrimLE,
                LoadOrderFilePath = Utility.PathToLoadOrderFile,
                OutputPath = output,
                SourcePath = null
            };
            using var state = Mutagen.Bethesda.Synthesis.Internal.Utility.ToState(
                GameCategory.Skyrim,
                settings,
                new PatcherPreferences(),
                output.ModKey);
            state.RawLoadOrder.Should().HaveCount(3);
            state.RawLoadOrder.Select(l => l.ModKey).Should().Equal(new ModKey[]
            {
                Utility.TestFileName,
                Utility.OverrideModKey,
                output.ModKey,
            });
        }
    }
}
