using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Cli
{
    public interface ICliPatcherRun : IPatcherRun
    {
    }

    public class CliPatcherRun : ICliPatcherRun
    {
        private readonly CompositeDisposable _disposable = new();

        public string Name => _name.Name;
        public IProcessRunner ProcessRunner { get; }
        public IPathToExecutableInputProvider ExePath { get; }

        public ILogger Logger { get; }
        private readonly IPatcherNameProvider _name;
        public IGenericSettingsToMutagenSettings GenericToMutagenSettings { get; }
        public IFormatCommandLine Format { get; }

        public CliPatcherRun(
            ILogger logger,
            IProcessRunner processRunner,
            IPatcherNameProvider name,
            IPathToExecutableInputProvider exePath,
            IGenericSettingsToMutagenSettings genericToMutagenSettings,
            IFormatCommandLine format)
        {
            ProcessRunner = processRunner;
            ExePath = exePath;
            Logger = logger;
            _name = name;
            GenericToMutagenSettings = genericToMutagenSettings;
            Format = format;
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        [ExcludeFromCodeCoverage]
        public async Task Prep(CancellationToken cancel)
        {
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) return;

            var internalSettings = GenericToMutagenSettings.Convert(settings);
            var args = Format.Format(internalSettings);
            
            try
            {
                var result = await ProcessRunner.Run(
                    new ProcessStartInfo(ExePath.Path, args)
                    {
                        WorkingDirectory = ExePath.Path.Directory!
                    },
                    cancel);
                if (result != 0)
                {
                    throw new CliUnsuccessfulRunException(
                        result,
                        $"Process exited in failure: {ExePath.Path} {internalSettings}");
                }
            }
            catch (Win32Exception ex)
            {
                throw new FileNotFoundException($"Could not find target CLI file: {ExePath.Path}", ex);
            }
        }
    }
}
