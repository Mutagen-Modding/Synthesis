using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class PipelineTests
    {
        [Fact]
        public void AddsImplicitMods()
        {
            using var tmp = Utility.GetTempFolder();

            var pluginPath = Path.Combine(tmp.Dir.Path, "Plugins.txt");
            var dataFolder = Path.Combine(tmp.Dir.Path, "Data");
            Directory.CreateDirectory(dataFolder);
            File.WriteAllText(
                Path.Combine(dataFolder, Mutagen.Bethesda.Skyrim.Constants.Skyrim.FileName),
                string.Empty);
            File.WriteAllLines(pluginPath,
                new string[]
                {
                    $"*{Utility.TestModKey.FileName}",
                    $"{Utility.OverrideModKey.FileName}",
                });
            var listings = Mutagen.Bethesda.Synthesis.Internal.Utility.GetLoadOrder(
                GameRelease.SkyrimSE,
                loadOrderFilePath: pluginPath,
                dataFolderPath: dataFolder).ToList();
            listings.Should().HaveCount(3);
            listings.Should().BeEquivalentTo(new LoadOrderListing[]
            {
                new LoadOrderListing(Mutagen.Bethesda.Skyrim.Constants.Skyrim, true),
                new LoadOrderListing(Utility.TestModKey, true),
                new LoadOrderListing(Utility.OverrideModKey, false),
            });
        }

        [Fact]
        public void GetLoadOrder_NoLoadOrderPath()
        {
            using var tmpFolder = Utility.GetTempFolder(nameof(RunnerTests));
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimSE);
            var lo = Mutagen.Bethesda.Synthesis.Internal.Utility.GetLoadOrder(
                GameRelease.SkyrimSE, 
                string.Empty,
                dataFolder.Dir.Path);
            lo.Select(l => l.ModKey).Should().BeEmpty();
        }
    }
}
