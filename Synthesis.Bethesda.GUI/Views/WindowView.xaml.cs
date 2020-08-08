using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class WindowViewBase : NoggogUserControl<MainVM> { }

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
                this.WhenAnyValue(x => x.ViewModel.ActivePanel)
                    .BindToStrict(this, x => x.ContentPane.Content)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.ActiveConfirmation)
                    .Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.ConfirmationOverlay.Visibility)
                    .DisposeWith(disposable);
            });
        }
    }
}
