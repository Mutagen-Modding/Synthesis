using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using System.Reactive.Linq;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Views;

public class PatcherRunViewBase : NoggogUserControl<PatcherRunVm> { }

/// <summary>
/// Interaction logic for PatcherRunView.xaml
/// </summary>
public partial class PatcherRunView : PatcherRunViewBase
{
    public PatcherRunView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Name)
                .BindTo(this, x => x.PatcherDetailName.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.SourceVm)
                .BindTo(this, x => x.PatcherIconDisplay.DataContext)
                .DisposeWith(disposable);

            // Set state subheader
            this.WhenAnyValue(x => x.ViewModel!.State.Value)
                .Select(state =>
                {
                    return state switch
                    {
                        RunState.NotStarted => "Not Run",
                        RunState.Error => "Errored",
                        RunState.Finished => "Completed",
                        RunState.Started => "Running",
                        _ => throw new NotImplementedException()
                    };
                })
                .BindTo(this, x => x.StatusBlock.Text)
                .DisposeWith(disposable);

            // Set up text output
            this.WhenAnyValue(x => x.ViewModel!.OutputDisplay)
                .BindTo(this, x => x.OutputBox.Document)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.OutputDisplay.TextLength)
                .Select(count => count > 0 ? Visibility.Visible : Visibility.Hidden)
                .BindTo(this, x => x.OutputBox.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.OutputDisplay)
                .ObserveOnGui()
                .Delay(TimeSpan.FromMilliseconds(50), RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    OutputBox.SetValue(TextEditorEx.DoScrollOnChangeProperty, true);
                })
                .DisposeWith(disposable);
        });
    }
}