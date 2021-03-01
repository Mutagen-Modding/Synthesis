using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class DotNetNotInstalledVM : ViewModel
    {
        public ICommand DownloadCommand { get; }

        private readonly ObservableAsPropertyHelper<string> _CustomDisplayString;
        public string CustomDisplayString => _CustomDisplayString.Value;

        public DotNetNotInstalledVM(
            MainVM mvm,
            ViewModel goBack,
            IObservable<DotNetVersion> dotNetVersion)
        {
            dotNetVersion
                .Subscribe(v =>
                {
                    if (v.Acceptable)
                    {
                        mvm.ActivePanel = goBack;
                    }
                })
                .DisposeWith(this);

            _CustomDisplayString = dotNetVersion
                .Select(x =>
                {
                    if (x.Acceptable) return string.Empty;
                    if (x.Version.IsNullOrWhitespace())
                    {
                        return "While the app can open with the DotNet Runtime, it also needs the SDK to be able to function.";
                    }
                    else
                    {
                        return $"While an SDK was found, it was not an acceptable version.  You had {x.Version}, but it must be at least {DotNetCommands.MinVersion}";
                    }
                })
                .ToGuiProperty(this, nameof(CustomDisplayString), string.Empty);

            DownloadCommand = ReactiveCommand.Create(
                () =>
                {
                    Utility.NavigateToPath("https://dotnet.microsoft.com/download");
                });
        }
    }
}
