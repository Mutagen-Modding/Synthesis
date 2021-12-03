using Noggog.WPF;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatcherConfigViewBase : NoggogUserControl<PatcherVm> { }

    /// <summary>
    /// Interaction logic for PatcherConfigView.xaml
    /// </summary>
    public partial class PatcherConfigView : PatcherConfigViewBase
    {
        public PatcherConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.NameVm.Name)
                    .BindTo(this, x => x.PatcherDetailName.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindTo(this, x => x.PatcherIconDisplay.DataContext)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ErrorDisplayVm.DisplayedObject)
                    .BindTo(this, x => x.ConfigDetailPane.Content)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                    .BindTo(this, x => x.DeleteButton.Command)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.PatcherDetailName.IsKeyboardFocused)
                    .Select(focused =>
                    {
                        if (focused)
                        {
                            return this.WhenAnyValue(x => x.ViewModel!.NameVm.Name);
                        }
                        else
                        {
                            return this.WhenAnyValue(x => x.ViewModel!.NameVm.Name);
                        }
                    })
                    .Switch()
                    .DistinctUntilChanged()
                    .Subscribe(x => this.PatcherDetailName.Text = x)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.IsNameEditable)
                    .BindTo(this, x => x.PatcherDetailName.IsHitTestVisible)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.ErrorDisplayVm)
                    .BindTo(this, x => x.BottomErrorDisplay.DataContext)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.RenameCommand)
                    .BindTo(this, x => x.RenameButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
