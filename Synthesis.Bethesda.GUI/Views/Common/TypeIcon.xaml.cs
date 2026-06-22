using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace Synthesis.Bethesda.GUI.Views;

public class TypeIconBase : NoggogUserControl<object> { }

/// <summary>
/// Interaction logic for TypeIcon.xaml
/// </summary>
public partial class TypeIcon : TypeIconBase
{
    public TypeIcon()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .BindTo(this, x => x.ContentControl.Content)
                .DisposeWith(disposable);
        });
    }
}