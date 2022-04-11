using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI;

/// <summary>
/// Class to house some backend bindings for reuse
/// </summary>
public static class UtilityBindings
{
    #region Help Sections
    public static IDisposable HelpWiring(IShowHelpSetting showHelp, Button button, TextBlock helpBlock, IObservable<bool>? show = null)
    {
        CompositeDisposable ret = new();

        show ??= Observable.Return(true);

        show.Select(x => x ? Visibility.Visible : Visibility.Collapsed)
            .Subscribe(x => button.Visibility = x)
            .DisposeWith(ret);

        Observable.CombineLatest(
                show,
                showHelp.WhenAnyValue(x => x.ShowHelp),
                (newPatcher, on) => on && newPatcher)
            .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
            .Subscribe(x => helpBlock.Visibility = x)
            .DisposeWith(ret);

        showHelp.WhenAnyValue(x => x.ShowHelpToggleCommand)
            .Subscribe(x => button.Command = x)
            .DisposeWith(ret);

        showHelp.WhenAnyValue(x => x.ShowHelp)
            .Select(x => x ? 1.0d : 0.4d)
            .Subscribe(x => button.Opacity = x)
            .DisposeWith(ret);

        return ret;
    }
    #endregion
}