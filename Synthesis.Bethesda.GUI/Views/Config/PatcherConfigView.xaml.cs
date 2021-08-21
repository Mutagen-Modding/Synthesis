using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatcherConfigViewBase : NoggogUserControl<PatcherVM> { }

    /// <summary>
    /// Interaction logic for PatcherConfigView.xaml
    /// </summary>
    public partial class PatcherConfigView : PatcherConfigViewBase
    {
        public PatcherConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.DisplayName)
                    .BindTo(this, x => x.PatcherDetailName.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindTo(this, x => x.PatcherIconDisplay.DataContext)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DisplayedObject)
                    .BindTo(this, x => x.ConfigDetailPane.Content)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                    .BindTo(this, x => x.DeleteButton.Command)
                    .DisposeWith(disposable);

                // Hacky setup to edit nickname when focused, but display display name when not
                // Need to polish and redeploy EditableTextBox instead sometime
                this.WhenAnyValue(x => x.PatcherDetailName.Text)
                    .Skip(1)
                    .FilterSwitch(this.WhenAnyValue(x => x.PatcherDetailName.IsFocused))
                    .Subscribe(x =>
                    {
                        if (this.ViewModel is {} vm)
                        {
                            vm.Nickname = x;
                        }
                    })
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.PatcherDetailName.IsKeyboardFocused)
                    .Select(focused =>
                    {
                        if (focused)
                        {
                            return this.WhenAnyValue(x => x.ViewModel!.Nickname);
                        }
                        else
                        {
                            return this.WhenAnyValue(x => x.ViewModel!.DisplayName);
                        }
                    })
                    .Switch()
                    .DistinctUntilChanged()
                    .Subscribe(x => this.PatcherDetailName.Text = x)
                    .DisposeWith(disposable);

                var errorDisp = Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.State.IsHaltingError),
                        this.WhenAnyValue(x => x.ViewModel!.DisplayedObject),
                        (halting, disp) => halting && !(disp is ErrorVM))
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .Replay(1)
                    .RefCount();
                errorDisp
                    .BindTo(this, x => x.ErrorButton.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.GoToErrorCommand)
                    .BindTo(this, x => x.ErrorButton.Command)
                    .DisposeWith(disposable);
                errorDisp
                    .BindTo(this, x => x.ErrorGlow.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.State)
                    .Select(x => x.RunnableState.Reason)
                    .Select(x => x.Split(Environment.NewLine).FirstOrDefault())
                    .BindTo(this, x => x.ErrorTextBlock.Text)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.IsNameEditable)
                    .BindTo(this, x => x.PatcherDetailName.IsHitTestVisible)
                    .DisposeWith(disposable);
            });
        }
    }
}
