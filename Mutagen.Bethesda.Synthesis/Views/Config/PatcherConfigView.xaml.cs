using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
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

namespace Mutagen.Bethesda.Synthesis.Views
{
    public class PatcherConfigViewBase : NoggogUserControl<PatcherVM> { }

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
                this.WhenAnyValue(x => x.ViewModel.DisplayName)
                    .BindToStrict(this, x => x.PatcherDetailName.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindToStrict(this, x => x.PatcherIconDisplay.Content)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindToStrict(this, x => x.ConfigDetailPane.Content)
                    .DisposeWith(disposable);

                // Hacky setup to edit nickname when focused, but display display name when not
                // Need to polish and redeploy EditableTextBox instead sometime
                this.WhenAnyValue(x => x.PatcherDetailName.Text)
                    .Skip(1)
                    .FilterSwitch(this.WhenAnyValue(x => x.PatcherDetailName.IsFocused))
                    .Subscribe(x => this.ViewModel.Nickname = x)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.PatcherDetailName.IsKeyboardFocused)
                    .Select(focused =>
                    {
                        if (focused)
                        {
                            return this.WhenAnyValue(x => x.ViewModel.Nickname);
                        }
                        else
                        {
                            return this.WhenAnyValue(x => x.ViewModel.DisplayName);
                        }
                    })
                    .Switch()
                    .DistinctUntilChanged()
                    .Subscribe(x => this.PatcherDetailName.Text = x)
                    .DisposeWith(disposable);

                // Clear textbox keyboard focus on keybinds
                this.Events().KeyUp
                    .Where(k => k.Key == Key.Escape || k.Key == Key.Return)
                    .Unit()
                    .Subscribe(_ =>
                    {
                        Keyboard.ClearFocus();
                    })
                    .DisposeWith(disposable);
            });
        }
    }
}
