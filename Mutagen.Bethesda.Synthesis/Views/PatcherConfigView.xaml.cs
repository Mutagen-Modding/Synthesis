using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows;

namespace Mutagen.Bethesda.Synthesis.Views
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
                this.WhenAnyValue(x => x.ViewModel.IsSelected)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.SelectedGlow.Visibility)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.IsOn, view => view.OnCheckbox.IsChecked)
                    .DisposeWith(disposable);
            });
        }
    }
}
