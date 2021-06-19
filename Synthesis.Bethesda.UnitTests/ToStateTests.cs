using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using System.Linq;
using Mutagen.Bethesda.Core.Plugins.Order;
using Mutagen.Bethesda.Plugins.Masters;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Synthesis.States;
using Xunit;
using Path = System.IO.Path;

namespace Synthesis.Bethesda.UnitTests
{
    public class ToStateTests : IClassFixture<LoquiUse>
    {
        [Fact]
        public void LoadOrderTrim()
        {
            var env = Utility.SetupEnvironment(GameRelease.SkyrimLE);
            var output = Utility.TypicalOutputFile(env.DataFolder);
            var pluginPath = Path.Combine(env.DataFolder, "Plugins.txt");
            env.FileSystem.File.WriteAllLines(pluginPath,
                new string[]
                {
                    $"*{Utility.TestFileName}",
                    $"*{Utility.OverrideFileName}",
                    $"*{output.ModKey.FileName}",
                    $"*{Utility.OtherFileName}",
                });
            var settings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = env.DataFolder,
                GameRelease = GameRelease.SkyrimLE,
                LoadOrderFilePath = env.PluginPath,
                OutputPath = output,
                SourcePath = null
            };
            var stateFactory = GetStateFactory(env);
            using var state = stateFactory.ToState(
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

        private static StateFactory GetStateFactory(TestEnvironment env)
        {
            var stateFactory = new StateFactory(
                env.FileSystem,
                new LoadOrderImporter(env.FileSystem),
                new GetStateLoadOrder(env.FileSystem,
                    new PluginListingsRetriever(
                        env.FileSystem,
                        new TimestampAligner(env.FileSystem))),
                new EnableImplicitMasters(
                    new FindImplicitlyIncludedMods(
                        new MasterReferenceReaderFactory(env.FileSystem))));
            return stateFactory;
        }

        [Fact]
        public void NonSynthesisTarget()
        {
            var env = Utility.SetupEnvironment(GameRelease.SkyrimLE);
            var output = ModPath.FromPath(Path.Combine(env.DataFolder, Utility.OtherFileName));
            var pluginPath = Path.Combine(env.DataFolder, "Plugins.txt");
            env.FileSystem.File.WriteAllLines(pluginPath,
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
                DataFolderPath = env.DataFolder,
                GameRelease = GameRelease.SkyrimLE,
                LoadOrderFilePath = env.PluginPath,
                OutputPath = output,
                SourcePath = null
            };
            var stateFactory = GetStateFactory(env);
            using var state = stateFactory.ToState(
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
