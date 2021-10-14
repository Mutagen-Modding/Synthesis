using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Kernel;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;

namespace Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors
{
    public class NugetConfigErrorVm : ViewModel, IEnvironmentErrorVm
    {
        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<ErrorVM?> _Error;
        public ErrorVM? Error => _Error.Value;
        
        public FilePath NugetConfigPath { get; }

        private readonly ObservableAsPropertyHelper<string?> _ErrorString;
        public string? ErrorString => _ErrorString.Value;

        public NugetConfigErrorVm(
            IAnalyzeNugetConfig analyzeNugetConfig,
            ILogger logger)
        {
            NugetConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NuGet",
                "Nuget.Config");
            _Error = Noggog.ObservableExt.WatchFile(NugetConfigPath)
                .StartWith(Unit.Default)
                .Select(_ =>
                {
                    var err = analyzeNugetConfig.Analyze(NugetConfigPath);

                    if (err == null) return default(ErrorVM?);
                    
                    if (err is CorruptError corr)
                    {
                        logger.Error(corr.Exception, "Nuget.Config corrupt");
                    }

                    return new ErrorVM(this, logger, err);
                })
                .RetryWithBackOff<ErrorVM?, Exception>((_, times) => TimeSpan.FromMilliseconds(Math.Min(times * 250, 5000)))
                .ToGuiProperty(this, nameof(Error), default);
            _InError = this.WhenAnyValue(x => x.Error)
                .Select(x => x != null)
                .ToGuiProperty(this, nameof(InError));
            _ErrorString = this.WhenAnyValue(x => x.Error)
                .Select(x => x != null ? $"Nuget Config: {x.ErrorText}" : null)
                .ToGuiProperty(this, nameof(ErrorString), default);
        }

        public class ErrorVM
        {
            public string ErrorText { get; }
            public ICommand RunFix { get; }
            
            public ErrorVM(NugetConfigErrorVm parent, ILogger logger, INugetErrorSolution errSolution)
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
                        logger.Error(e, "Error executing nuget config fix");
                    }
                });
            }
        }
    }
}