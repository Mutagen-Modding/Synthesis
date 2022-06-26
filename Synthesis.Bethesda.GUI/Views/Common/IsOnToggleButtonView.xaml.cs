using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for IsOnToggleButtonView.xaml
/// </summary>
public partial class IsOnToggleButtonView : UserControl
{
    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }
    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(IsOnToggleButtonView),
        new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public IsOnToggleButtonView()
    {
        InitializeComponent();
    }
}