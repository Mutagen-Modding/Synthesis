using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI
{
    /// <summary>
    /// Class to house some backend bindings for reuse
    /// </summary>
    public static class UtilityBindings
    {
        #region Help Sections
        public static IDisposable HelpWiring(PatcherVM vm, Button button, TextBlock helpBlock)
        {
            CompositeDisposable ret = new CompositeDisposable();

            var isNewPatcher = vm.WhenAnyFallback(x => x.Profile.Config.NewPatcher, default)
                .Select(newPatcher => object.ReferenceEquals(newPatcher, vm))
                .Replay(1)
                .RefCount();

            isNewPatcher
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .Subscribe(x => button.Visibility = x)
                .DisposeWith(ret);

            Observable.CombineLatest(
                    isNewPatcher,
                    vm.WhenAnyValue(x => x.Profile.Config.ShowHelp),
                    (newPatcher, on) => on && newPatcher)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .Subscribe(x => helpBlock.Visibility = x)
                .DisposeWith(ret);

            vm.WhenAnyValue(x => x.ShowHelpCommand)
                .Subscribe(x => button.Command = x)
                .DisposeWith(ret);

            vm.WhenAnyValue(x => x.Profile.Config.ShowHelp)
                .Select(x => x ? 1.0d : 0.4d)
                .Subscribe(x => button.Opacity = x)
                .DisposeWith(ret);

            return ret;
        }
        #endregion
    }
}
