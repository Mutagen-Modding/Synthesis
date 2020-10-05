using Noggog.WPF;
using Noggog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatcherConfigListingViewBase : NoggogUserControl<PatcherVM> { }

    /// <summary>
    /// Interaction logic for PatcherConfigListingView.xaml
    /// </summary>
    public partial class PatcherConfigListingView : PatcherConfigListingViewBase
    {
        public PatcherConfigListingView()
        {
            InitializeComponent();
            this.WhenActivated((disposable) =>
            {
                this.WhenAnyFallback(x => x.ViewModel!.IsSelected)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.SelectedGlow.Visibility)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.IsOn, view => view.OnToggle.IsChecked)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.DisplayName)
                    .BindToStrict(this, x => x.NameBlock.Text)
                    .DisposeWith(disposable);

                // Set up blocking issue display
                this.WhenAnyFallback(x => x.ViewModel!.State.RunnableState.Succeeded)
                    .Select(x => x ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.BlockingIssueDisplayCircle.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.State.RunnableState.Reason)
                    .BindToStrict(this, x => x.BlockingIssueDisplayCircle.ToolTip)
                    .DisposeWith(disposable);

                // ContextMenu
                this.WhenAnyFallback(x => x.ViewModel!.DeleteCommand)
                    .BindToStrict(this, x => x.DeleteContextMenuButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
