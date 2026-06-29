using System.Reactive.Linq;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

/// <summary>
/// Reports whether the app is in "MO2 prep mode": MO2 mode is enabled but Synthesis is running
/// standalone (outside MO2). In this state the app exists only to build/prepare patchers, so the
/// run buttons are replaced with build-status displays.
/// </summary>
public interface IMo2PrepModeProvider
{
    /// <summary>Current snapshot of whether MO2 prep mode is active.</summary>
    bool Active { get; }

    /// <summary>Live stream of <see cref="Active"/>, emitting the current value on subscription.</summary>
    IObservable<bool> ActiveObservable { get; }
}

public class Mo2PrepModeProvider : IMo2PrepModeProvider
{
    private readonly GlobalSettingsVm _globalSettings;
    private readonly bool _insideMo2;

    public IObservable<bool> ActiveObservable { get; }

    public Mo2PrepModeProvider(
        GlobalSettingsVm globalSettings,
        IMo2EnvironmentDetector mo2Detector)
    {
        _globalSettings = globalSettings;
        _insideMo2 = mo2Detector.IsRunningInsideMo2();
        ActiveObservable = globalSettings.WhenAnyValue(x => x.BlockBuildingWithinMo2)
            .Select(block => block && !_insideMo2)
            .DistinctUntilChanged();
    }

    public bool Active => _globalSettings.BlockBuildingWithinMo2 && !_insideMo2;
}
