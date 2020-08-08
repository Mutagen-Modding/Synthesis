using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

                this.WhenAnyValue(x => x.ViewModel.CompilationText)
                    .BindToStrict(this, x => x.CompileResultsBlock.Text)
                    .DisposeWith(disposable);

                // When hovering over error text, enable wrapping so that it expands to show all
                // Delay turning wrap off by a bit, just to make it feel better
                this.WhenAnyValue(x => x.CompileResultsBlock.IsMouseOver)
                    .Skip(1)
                    .Select(isOver =>
                    {
                        if (isOver) return Observable.Return(TextWrapping.Wrap);
                        return Observable.Return(TextWrapping.NoWrap)
                            .Delay(TimeSpan.FromMilliseconds(300), RxApp.MainThreadScheduler);
                    })
                    .Switch()
                    .StartWith(TextWrapping.NoWrap)
                    .BindToStrict(this, x => x.CompileResultsBlock.TextWrapping)
                    .DisposeWith(disposable);
            });
        }
    }
}
