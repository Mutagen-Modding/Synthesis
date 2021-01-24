using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
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
    public class RequiredModsViewBase : NoggogUserControl<RequiredModsVM> { }

    /// <summary>
    /// Interaction logic for RequiredModsView.xaml
    /// </summary>
    public partial class RequiredModsView : RequiredModsViewBase
    {
        public string RequiredModsLabel
        {
            get => (string)GetValue(RequiredModsLabelProperty);
            set => SetValue(RequiredModsLabelProperty, value);
        }
        public static readonly DependencyProperty RequiredModsLabelProperty = DependencyProperty.Register(nameof(RequiredModsLabel), typeof(string), typeof(RequiredModsView),
             new FrameworkPropertyMetadata("Added Mods", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public RequiredModsView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.AddRequiredModKey, view => view.AddRequiredModBox.ModKey)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RequiredModsDisplay)
                    .BindToStrict(this, v => v.RequiredModsList.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.AddRequiredModCommand)
                    .BindToStrict(this, x => x.AddRequiredModButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DetectedMods)
                    .BindToStrict(this, v => v.DetectedModsList.ItemsSource)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.DetectedModsSearch, view => view.SearchBox.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ClearSearchCommand)
                    .BindToStrict(this, x => x.ClearSearchButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
