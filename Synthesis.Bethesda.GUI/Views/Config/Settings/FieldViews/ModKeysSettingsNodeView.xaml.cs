using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
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
    public class ModKeysSettingsNodeViewBase : NoggogUserControl<EnumerableModKeySettingsVM> { }

    /// <summary>
    /// Interaction logic for ModKeysSettingsNodeView.xaml
    /// </summary>
    public partial class ModKeysSettingsNodeView : ModKeysSettingsNodeViewBase
    {
        public ModKeysSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.MemberName)
                    .BindToStrict(this, x => x.SettingsNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.AddedModsVM)
                    .BindToStrict(this, x => x.RequiredModsPicker.DataContext)
                    .DisposeWith(disposable);
            });
        }
    }
}
