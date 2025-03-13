using Shouldly;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog.Testing.Extensions;
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
        state.RawLoadOrder.ShouldHaveCount(3);
        state.RawLoadOrder.Select(l => l.ModKey).ShouldEqual(new ModKey[]
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
        state.RawLoadOrder.ShouldHaveCount(3);
        state.RawLoadOrder.Select(l => l.ModKey).ShouldEqual(new ModKey[]
        {
            Utility.TestFileName,
            Utility.OverrideModKey,
            output.ModKey,
        });
    }
}