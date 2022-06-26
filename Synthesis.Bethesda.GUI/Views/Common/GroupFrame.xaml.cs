using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for GroupFrame.xaml
/// </summary>
public partial class GroupFrame : UserControl
{
    public bool Hovered
    {
        get => (bool)GetValue(HoveredProperty);
        set => SetValue(HoveredProperty, value);
    }
    public static readonly DependencyProperty HoveredProperty = DependencyProperty.Register(nameof(Hovered), typeof(bool), typeof(GroupFrame),
        new FrameworkPropertyMetadata(default(bool)));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(GroupFrame),
        new FrameworkPropertyMetadata(default(bool)));

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }
    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(GroupFrame),
        new FrameworkPropertyMetadata(default(bool)));

    public GroupFrame()
    {
        InitializeComponent();
    }
}