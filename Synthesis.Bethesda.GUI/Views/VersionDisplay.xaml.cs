using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    public class VersionDisplayBase : NoggogUserControl<MainVM> { }

    /// <summary>
    /// Interaction logic for VersionDisplay.xaml
    /// </summary>
    public partial class VersionDisplay : VersionDisplayBase
    {
        public VersionDisplay()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersion)
                    .BindToStrict(this, v => v.VersionButton.Content)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersion)
                    .BindToStrict(this, v => v.CurrentSynthesisVersionText.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.NewestSynthesisVersion)
                    .Select(x => x ?? "[Unknown]")
                    .BindToStrict(this, v => v.LatestSynthesisVersionText.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.MutagenVersion)
                    .BindToStrict(this, v => v.CurrentMutagenVersionText.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.NewestMutagenVersion)
                    .Select(x => x ?? "[Unknown]")
                    .BindToStrict(this, v => v.LatestMutagenVersionText.Text)
                    .DisposeWith(dispose);
                this.VersionButton.Events()
                    .PreviewMouseLeftButtonUp
                    .Subscribe(async (_) =>
                    {
                        this.VersionButton.Visibility = Visibility.Hidden;
                        this.CopiedText.Visibility = Visibility.Visible;
                        Clipboard.SetText($"Synthesis version: {this.ViewModel?.SynthesisVersion}\n" +
                            $"Mutagen version: {this.ViewModel?.MutagenVersion}");
                        await Task.Delay(900);
                        this.VersionButton.Visibility = Visibility.Visible;
                        this.CopiedText.Visibility = Visibility.Collapsed;
                    })
                    .DisposeWith(dispose);
            });
        }
    }
}
