using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Synthesis.Bethesda.GUI.Views
{
    public class NoProfileViewBase : NoggogUserControl<NewProfileVM> { }

    /// <summary>
    /// Interaction logic for NoProfileView.xaml
    /// </summary>
    public partial class NoProfileView : NoProfileViewBase
    {
        public NoProfileView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.ReleaseOptions)
                    .BindToStrict(this, x => x.GameReleaseOptionsControl.ItemsSource)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.SelectedGame, view => view.GameReleaseOptionsControl.SelectedItem)
                    .DisposeWith(disposable);
            });
        }
    }
}
