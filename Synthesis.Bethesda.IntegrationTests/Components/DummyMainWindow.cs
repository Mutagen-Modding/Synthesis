using System.ComponentModel;
using System.Windows;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.GUI.Views;

namespace Synthesis.Bethesda.IntegrationTests.Components;

public class DummyMainWindow : IMainWindow, IWindowPlacement
{
    public Visibility Visibility { get; set; }
    public object DataContext { get; set; } = null!;
    public event CancelEventHandler? Closing;
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}