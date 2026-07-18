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

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ReadMoreButton),
            new PropertyMetadata("Read More"));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty ShowDownloadIconProperty =
        DependencyProperty.Register(
            nameof(ShowDownloadIcon),
            typeof(bool),
            typeof(ReadMoreButton),
            new PropertyMetadata(false));

    /// <summary>
    /// When true, shows a Material "Download" icon instead of the default "Read More" book icon.
    /// </summary>
    public bool ShowDownloadIcon
    {
        get => (bool)GetValue(ShowDownloadIconProperty);
        set => SetValue(ShowDownloadIconProperty, value);
    }

    public ReadMoreButton()
    {
        InitializeComponent();
    }
}
