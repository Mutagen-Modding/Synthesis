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
            this.WhenActivated((vm, disposable) =>
            {
                vm.WhenAnyValue(x => x.IsSelected)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.SelectedGlow.Visibility)
                    .DisposeWith(disposable);
                this.BindStrict(vm, vm => vm.IsOn, view => view.OnToggle.IsChecked)
                    .DisposeWith(disposable);
                vm.WhenAnyValue(x => x.DisplayName)
                    .BindToStrict(this, x => x.NameBlock.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
