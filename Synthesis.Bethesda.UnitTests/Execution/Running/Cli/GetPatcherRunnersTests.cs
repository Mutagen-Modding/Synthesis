using Shouldly;
using Noggog.NSubstitute;
using Noggog.Testing.Extensions;
using NSubstitute;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Cli;

public class GetPatcherRunnersTests
{
    [Theory, SynthAutoData]
    public void PassesPatchersToRunnerFactory(
        List<PatcherSettings> settings,
        GetPatcherRunners sut)
    {
        settings.ForEach(x => x.On = true);
        sut.Get(settings);
        foreach (var setting in settings)
        {
            sut.PatcherSettingsToRunnerFactory.Received(1).Convert(setting);
        }
    }
        
    [Theory, SynthAutoData]
    public void OnlyIncludesOnPatchers(
        List<PatcherSettings> settings,
        GetPatcherRunners sut)
    {
        settings.ForEach(x => x.On = true);
        settings[^1].On = false;
        sut.Get(settings);
        foreach (var setting in settings.Where(x => x.On))
        {
            sut.PatcherSettingsToRunnerFactory.Received(1).Convert(setting);
        }
    }
        
    [Theory, SynthAutoData]
    public void ReturnsPatchers(
        List<PatcherSettings> settings,
        IPatcherRun[] patcherRuns,
        GetPatcherRunners sut)
    {
        settings.ForEach(x => x.On = true);
        sut.PatcherSettingsToRunnerFactory.Convert(default!)
            .ReturnsSeriallyForAnyArgs(patcherRuns);
        sut.Get(settings).ShouldEqual(patcherRuns);
    }
}