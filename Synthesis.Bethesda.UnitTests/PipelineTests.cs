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
            var testEnv = new TestEnvironment(
                IFileSystemExt.DefaultFilesystem,
                GameRelease.SkyrimSE,
                string.Empty,
                dataFolder,
                pluginPath);
            var getStateLoadOrder = testEnv.GetStateLoadOrder();
            var listings = getStateLoadOrder.GetLoadOrder(false).ToList();
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
            var env = Utility.SetupEnvironment(GameRelease.SkyrimSE);
            env = env with {PluginPath = string.Empty};
            var getStateLoadOrder = env.GetStateLoadOrder();
            var lo = getStateLoadOrder.GetLoadOrder(false);
            lo.Select(l => l.ModKey).Should().BeEmpty();
        }
    }
}
