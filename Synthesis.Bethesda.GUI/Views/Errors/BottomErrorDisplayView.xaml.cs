using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using System.Windows;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Views
{
    public class BottomErrorDisplayViewBase : NoggogUserControl<ErrorDisplayVm> { }
    
    public partial class BottomErrorDisplayView : BottomErrorDisplayViewBase
    {
        public BottomErrorDisplayView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
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
                    .Select(x => x?.Split(Environment.NewLine).FirstOrDefault())
                    .BindTo(this, x => x.ErrorTextBlock.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}