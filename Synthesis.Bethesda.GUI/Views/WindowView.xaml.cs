using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views
{
    public class WindowViewBase : NoggogUserControl<MainVm> { }

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class WindowView : WindowViewBase
    {
        public WindowView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.ActivePanel),
                        this.WhenAnyValue(x => x.ViewModel!.EnvironmentErrors.ActiveError),
                        (active, err) => ((object?)err) ?? active)
                    .BindTo(this, x => x.ContentPane.Content)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ActiveConfirmation)
                    .Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.ConfirmationOverlay.Visibility)
                    .DisposeWith(disposable);
            });
        }
    }
}
