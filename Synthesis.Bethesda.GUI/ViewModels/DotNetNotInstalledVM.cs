using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class DotNetNotInstalledVM : ViewModel
    {
        public ICommand DownloadCommand { get; }

        public DotNetNotInstalledVM(
            MainVM mvm,
            ViewModel goBack,
            IObservable<Version?> dotNetVersion)
        {
            dotNetVersion
                .Subscribe(v =>
                {
                    if (v != null)
                    {
                        mvm.ActivePanel = goBack;
                    }
                })
                .DisposeWith(this);

            DownloadCommand = ReactiveCommand.Create(
                () =>
                {
                    Utility.NavigateToPath("https://dotnet.microsoft.com/download");
                });
        }
    }
}
