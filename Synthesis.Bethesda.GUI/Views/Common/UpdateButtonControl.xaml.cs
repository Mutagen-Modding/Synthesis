using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI.Views
{
    /// <summary>
    /// Interaction logic for UpdateButtonControl.xaml
    /// </summary>
    public partial class UpdateButtonControl : UserControl
    {
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(UpdateButtonControl),
             new FrameworkPropertyMetadata(default(ICommand)));

        public UpdateButtonControl()
        {
            InitializeComponent();
        }
    }
}
