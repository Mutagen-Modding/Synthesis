using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
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

namespace Synthesis.Bethesda.GUI.Views
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
            this.WhenActivated(disposable =>
            {
                this.Bind(this.ViewModel, vm => vm.Code, v => v.CodeBox.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
