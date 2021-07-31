using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution;
using System.Reactive.Linq;
using System.Windows.Input;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI
{
    public class DotNetNotInstalledVm : ViewModel, IEnvironmentErrorVm
    {
        public ICommand DownloadCommand { get; }

        private readonly ObservableAsPropertyHelper<string> _CustomDisplayString;
        public string CustomDisplayString => _CustomDisplayString.Value;

        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<string?> _ErrorString;
        public string? ErrorString => _ErrorString.Value;
        
        public DotNetNotInstalledVm(IInstalledSdkFollower mvm, INavigateTo navigate)
        {
            _InError = mvm.DotNetSdkInstalled
                .Select(x => !x.Acceptable)
                .ToGuiProperty(this, nameof(InError));
            
            _CustomDisplayString = mvm.DotNetSdkInstalled
                .Select(x =>
                {
                    if (x.Acceptable) return string.Empty;
                    if (x.Version.IsNullOrWhitespace())
                    {
                        return "While the app can open with the DotNet Runtime, it also needs the SDK to be able to function.";
                    }
                    else
                    {
                        return $"While an SDK was found, it was not an acceptable version.  You had {x.Version}, but it must be at least {ParseNugetVersionString.MinVersion}";
                    }
                })
                .ToGuiProperty(this, nameof(CustomDisplayString), string.Empty);

            DownloadCommand = ReactiveCommand.Create(
                () =>
                {
                    navigate.Navigate("https://dotnet.microsoft.com/download");
                });
            
            _ErrorString = this.WhenAnyValue(x => x.InError)
                .Select(x => x ? $"DotNet SDK: Desired SDK not found" : null)
                .ToGuiProperty(this, nameof(ErrorString), default);
        }
    }
}
