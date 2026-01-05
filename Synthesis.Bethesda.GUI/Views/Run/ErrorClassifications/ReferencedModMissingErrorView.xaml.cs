using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Synthesis.Bethesda.GUI.Views.ErrorClassifications;

/// <summary>
/// Interaction logic for ReferencedModMissingErrorView.xaml
/// </summary>
public partial class ReferencedModMissingErrorView : UserControl
{
    public ReferencedModMissingErrorView()
    {
        InitializeComponent();
        ReadMoreLink.RequestNavigate += (sender, e) =>
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        };
    }
}
