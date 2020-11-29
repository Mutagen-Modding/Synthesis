using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    /// <summary>
    /// Interaction logic for NugetVersioningView.xaml
    /// </summary>
    public partial class NugetVersioningView : UserControl
    {
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NugetVersioningView),
             new FrameworkPropertyMetadata(default(string)));

        public NugetVersioningView()
        {
            InitializeComponent();
        }
    }
}
