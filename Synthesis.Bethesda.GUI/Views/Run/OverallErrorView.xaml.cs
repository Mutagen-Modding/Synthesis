using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    public class OverallErrorViewBase : NoggogUserControl<OverallErrorVM> { }

    /// <summary>
    /// Interaction logic for OverallErrorView.xaml
    /// </summary>
    public partial class OverallErrorView : OverallErrorViewBase
    {
        public OverallErrorView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Exception)
                    .Select(x => x.ToString())
                    .BindToStrict(this, x => x.ErrorOutputBox.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
