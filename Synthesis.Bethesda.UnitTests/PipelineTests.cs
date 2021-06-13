using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using System.IO;
using System.Linq;
using Noggog;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class PipelineTests
    {
        [Fact]
        public void AddsImplicitMods()
        {
            using var tmp = Utility.GetTempFolder(nameof(PipelineTests));

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
                fileSystem: IFileSystemExt.DefaultFilesystem,
                GameRelease.SkyrimSE,
                loadOrderFilePath: pluginPath,
                dataFolderPath: dataFolder).ToList();
            listings.Should().HaveCount(3);
            listings.Should().BeEquivalentTo(new IModListingGetter[]
            {
                new ModListing(Mutagen.Bethesda.Skyrim.Constants.Skyrim, true),
                new ModListing(Utility.TestModKey, true),
                new ModListing(Utility.OverrideModKey, false),
            });
        }

        [Fact]
        public void GetLoadOrder_NoLoadOrderPath()
        {
            using var tmpFolder = Utility.GetTempFolder(nameof(PipelineTests));
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimSE);
            var lo = Mutagen.Bethesda.Synthesis.Internal.Utility.GetLoadOrder(
                fileSystem: IFileSystemExt.DefaultFilesystem,
                GameRelease.SkyrimSE, 
                string.Empty,
                dataFolder.Dir.Path);
            lo.Select(l => l.ModKey).Should().BeEmpty();
        }
    }
}
