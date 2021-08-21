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
    public class ConfigurationViewBase : NoggogUserControl<ConfigurationVM> { }

    /// <summary>
    /// Interaction logic for ConfigurationView.xaml
    /// </summary>
    public partial class ConfigurationView : ConfigurationViewBase
    {
        public ConfigurationView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyFallback(x => x.ViewModel!.NewPatcher)
                    .Select(x => x == null ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.Patchers.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.NewPatcher)
                    .Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.Initialization.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.NewPatcher)
                    .BindTo(this, x => x.Initialization.ViewModel)
                    .DisposeWith(dispose);
            });
        }
    }
}
