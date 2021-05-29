using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Kernel;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;

namespace Synthesis.Bethesda.GUI
{
    public class NugetConfigErrorVM : ViewModel
    {
        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<ErrorVM?> _Error;
        public ErrorVM? Error => _Error.Value;
        
        public FilePath NugetConfigPath { get; }

        public NugetConfigErrorVM()
        {
            NugetConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NuGet",
                "Nuget.Config");
            _Error = Noggog.ObservableExt.WatchFile(NugetConfigPath)
                .StartWith(Unit.Default)
                .Select(_ =>
                {
                    var err = NugetErrors.AnalyzeNugetConfig(NugetConfigPath);

                    if (err == null) return default(ErrorVM?);
                    
                    if (err is CorruptError corr)
                    {
                        Log.Logger.Error("Nuget.Config corrupt", corr.Exception);
                    }

                    return new ErrorVM(this, err);
                })
                .RetryWithBackOff<ErrorVM?, Exception>((_, times) => TimeSpan.FromMilliseconds(Math.Min(times * 250, 5000)))
                .ToGuiProperty(this, nameof(Error), default);
            _InError = this.WhenAnyValue(x => x.Error)
                .Select(x => x != null)
                .ToGuiProperty(this, nameof(InError));
        }

        public class ErrorVM
        {
            public string ErrorText { get; }
            public ICommand RunFix { get; }
            
            public ErrorVM(NugetConfigErrorVM parent, INugetErrorSolution errSolution)
            {
                ErrorText = errSolution.ErrorText;
                RunFix = ReactiveCommand.Create(() =>
                {
                    try
                    {
                        errSolution.RunFix(parent.NugetConfigPath);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("Error executing nuget config fix", e);
                    }
                });
            }
        }
    }
}