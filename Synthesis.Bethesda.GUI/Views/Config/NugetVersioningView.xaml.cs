using Noggog;
using Noggog.WPF;
using System;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class NugetVersioningViewBase : NoggogUserControl<INugetVersioningVM> { }

    /// <summary>
    /// Interaction logic for NugetVersioningView.xaml
    /// </summary>
    public partial class NugetVersioningView : NugetVersioningViewBase
    {
        public NugetVersioningView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.MutagenVersioning, view => view.MutagenVersioningTab.SelectedIndex, (e) => (int)e, i => (NugetVersioningEnum)i)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.ManualMutagenVersion, view => view.MutagenManualVersionBox.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.MutagenVersioning)
                    .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.MutagenManualVersionBox.Visibility)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.SynthesisVersioning, view => view.SynthesisVersioningTab.SelectedIndex, (e) => (int)e, i => (NugetVersioningEnum)i)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.ManualSynthesisVersion, view => view.SynthesisManualVersionBox.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ManualSynthesisVersion)
                    .Select(x => x.IsNullOrWhitespace())
                    .Subscribe(x => this.SynthesisManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ManualMutagenVersion)
                    .Select(x => x.IsNullOrWhitespace())
                    .Subscribe(x => this.MutagenManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersioning)
                    .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.SynthesisManualVersionBox.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.UsedMutagenVersion)
                    .Select(x =>
                    {
                        if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                        if (x.SelectedVersion != null && x.MatchVersion != null) return $"{x.MatchVersion} -> {x.SelectedVersion}";
                        return x.SelectedVersion ?? x.MatchVersion;
                    })
                    .BindToStrict(this, x => x.MutagenVersionText.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.UsedSynthesisVersion)
                    .Select(x =>
                    {
                        if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                        if (x.SelectedVersion != null && x.MatchVersion != null) return $"{x.MatchVersion} -> {x.SelectedVersion}";
                        return x.SelectedVersion ?? x.MatchVersion;
                    })
                    .BindToStrict(this, x => x.SynthesisVersionText.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
