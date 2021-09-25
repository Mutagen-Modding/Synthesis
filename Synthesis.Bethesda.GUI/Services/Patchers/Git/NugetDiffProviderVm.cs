using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface INugetDiffProviderVm
    {
        NugetVersionDiff MutagenVersionDiff { get; }
        NugetVersionDiff SynthesisVersionDiff { get; }
    }

    public class NugetDiffProviderVm : ViewModel, INugetDiffProviderVm
    {
        private readonly ObservableAsPropertyHelper<NugetVersionDiff> _MutagenVersionDiff;
        public NugetVersionDiff MutagenVersionDiff => _MutagenVersionDiff.Value;

        private readonly ObservableAsPropertyHelper<NugetVersionDiff> _SynthesisVersionDiff;
        public NugetVersionDiff SynthesisVersionDiff => _SynthesisVersionDiff.Value;

        public NugetDiffProviderVm(
            IRunnableStateProvider runnableStateProvider,
            IGitNugetTargetingVm nugetTargetingVm)
        {
            var cleanState = runnableStateProvider.WhenAnyValue(x => x.State)
                .Select(x => x.Item ?? default(RunnerRepoInfo?));

            _MutagenVersionDiff = Observable.CombineLatest(
                    cleanState.Select(x => x?.ListedVersions.Mutagen),
                    nugetTargetingVm.ActiveNugetVersion.Select(x => x.Value?.Mutagen.Version),
                    (matchVersion, selVersion) => new NugetVersionDiff(matchVersion, selVersion))
                .ToGuiProperty(this, nameof(MutagenVersionDiff), new NugetVersionDiff(null, null));

            _SynthesisVersionDiff = Observable.CombineLatest(
                    cleanState.Select(x => x?.ListedVersions.Synthesis),
                    nugetTargetingVm.ActiveNugetVersion.Select(x => x.Value?.Synthesis.Version),
                    (matchVersion, selVersion) => new NugetVersionDiff(matchVersion, selVersion))
                .ToGuiProperty(this, nameof(SynthesisVersionDiff), new NugetVersionDiff(null, null));
        }
    }
}