using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
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
    public class TypeIconBase : NoggogUserControl<object> { }

    /// <summary>
    /// Interaction logic for TypeIcon.xaml
    /// </summary>
    public partial class TypeIcon : TypeIconBase
    {
        public TypeIcon()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .BindTo(this, x => x.ContentControl.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
