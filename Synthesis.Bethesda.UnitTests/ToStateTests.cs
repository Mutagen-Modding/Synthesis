using System.Linq;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Implicit.DI;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Mutagen.Bethesda.Plugins.Masters;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Synthesis.States;
using Mutagen.Bethesda.Synthesis.States.DI;
using Xunit;
using Path = System.IO.Path;

namespace Synthesis.Bethesda.UnitTests;

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
        var stateFactory = env.GetStateFactory();
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
        var stateFactory = env.GetStateFactory();
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