using Noggog.WPF;
using System;
using System.Collections.Generic;
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

namespace Mutagen.Bethesda.Synthesis.Views
{
    public class CodeSnippetConfigViewBase : NoggogUserControl<CodeSnippetPatcherVM> { }

    /// <summary>
    /// Interaction logic for CodeSnippetConfigView.xaml
    /// </summary>
    public partial class CodeSnippetConfigView : CodeSnippetConfigViewBase
    {
        public CodeSnippetConfigView()
        {
            InitializeComponent();
        }
    }
}
