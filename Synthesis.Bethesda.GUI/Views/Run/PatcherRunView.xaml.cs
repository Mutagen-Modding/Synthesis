using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Windows;
using System.Reactive.Linq;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for PatcherRunView.xaml
/// </summary>
public partial class PatcherRunView
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

            // Determine if we should show tabs (when ErrorClassification exists)
            var hasErrorClassification = this.WhenAnyValue(x => x.ViewModel!.ErrorClassification)
                .Select(classification => classification != null);

            // Show/hide tabbed vs non-tabbed views
            hasErrorClassification
                .Select(hasClassification => hasClassification ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.ErrorTabs.Visibility)
                .DisposeWith(disposable);

            // Non-tabbed output box visibility (when no error classification AND has output)
            this.WhenAnyValue(
                    x => x.ViewModel!.ErrorClassification,
                    x => x.ViewModel!.OutputDisplay.TextLength,
                    (classification, textLength) => classification == null && textLength > 0)
                .Select(shouldShow => shouldShow ? Visibility.Visible : Visibility.Hidden)
                .BindTo(this, x => x.OutputBox.Visibility)
                .DisposeWith(disposable);

            // Set up non-tabbed text output
            this.WhenAnyValue(x => x.ViewModel!.OutputDisplay)
                .BindTo(this, x => x.OutputBox.Document)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.OutputDisplay)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Delay(TimeSpan.FromMilliseconds(50), RxSchedulers.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    OutputBox.SetValue(TextEditorEx.DoScrollOnChangeProperty, true);
                })
                .DisposeWith(disposable);

            // Set up tabbed console output
            this.WhenAnyValue(x => x.ViewModel!.OutputDisplay)
                .BindTo(this, x => x.OutputBoxTabbed.Document)
                .DisposeWith(disposable);

            // Bind error classification to ContentControl
            this.WhenAnyValue(x => x.ViewModel!.ErrorClassification)
                .BindTo(this, x => x.ErrorClassificationContent.Content)
                .DisposeWith(disposable);

            // Set Error Report tab as default when tabs are visible
            hasErrorClassification
                .Where(hasClassification => hasClassification)
                .Subscribe(_ =>
                {
                    ErrorTabs.SelectedIndex = 0; // Error Report tab
                })
                .DisposeWith(disposable);
        });
    }
}