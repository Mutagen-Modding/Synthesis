using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI.Views.Common;

/// <summary>
/// Reusable "Read More" button with GitHub icon and gradient hover effect
/// </summary>
public partial class ReadMoreButton : UserControl
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(ReadMoreButton),
            new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public ReadMoreButton()
    {
        InitializeComponent();
    }
}
