using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for GroupConfigView.xaml
/// </summary>
public partial class GroupConfigView
{
    public GroupConfigView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.Bind(this.ViewModel, x => x.Name, x => x.GroupDetailName.Text)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.ModKey)
                .Select(x => x.Failed)
                .BindTo(this, x => x.GroupDetailName.InError)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.ModKey)
                .Select(x => x.Reason)
                .BindTo(this, x => x.GroupDetailName.ErrorText)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                .BindTo(this, x => x.DeleteButton.Command)
                .DisposeWith(disposable);

            this.WhenAnyFallback(x => x.ViewModel!.State)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.MainThreadScheduler)
                .Select(state =>
                {
                    if (state.IsHaltingError) return "Blocking Error";
                    if (state.RunnableState.Failed) return "Preparing";
                    return "Ready";
                })
                .BindTo(this, x => x.StatusBlock.Text)
                .DisposeWith(disposable);
                
            this.WhenAnyValue(x => x.ViewModel!.ErrorDisplayVm)
                .BindTo(this, x => x.BottomErrorDisplayView.DataContext)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.ErrorDisplayVm.DisplayedObject)
                .BindTo(this, x => x.ConfigDetailPane.Content)
                .DisposeWith(disposable);
        });
    }
}