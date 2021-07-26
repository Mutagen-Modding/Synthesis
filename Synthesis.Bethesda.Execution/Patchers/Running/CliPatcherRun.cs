using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.Execution.Patchers.Running
{
    public interface ICliPatcherRun : IPatcherRun
    {
    }

    public class CliPatcherRun : ICliPatcherRun
    {
        private readonly CompositeDisposable _disposable = new();

        public string Name => _Name.Name;
        public IPathToExecutableInputProvider ExePath { get; }

        private readonly Subject<string> _output = new();
        public IObservable<string> Output => _output;

        private readonly Subject<string> _error = new();
        public IObservable<string> Error => _error;

        private readonly IProcessFactory _ProcessFactory;
        private readonly IPatcherNameProvider _Name;
        private readonly IPatcherExtraDataPathProvider _ExtraDataPathProvider;

        public delegate ICliPatcherRun Factory(string nickname, string pathToExecutable, string? pathToExtra);

        public CliPatcherRun(
            IProcessFactory processFactory,
            IPatcherNameProvider name,
            IPathToExecutableInputProvider exePath,
            IPatcherExtraDataPathProvider extraDataPathProvider)
        {
            ExePath = exePath;
            _ProcessFactory = processFactory;
            _Name = name;
            _ExtraDataPathProvider = extraDataPathProvider;
        }

        public void AddForDisposal(IDisposable disposable)
        {
            _disposable.Add(disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        public async Task Prep(GameRelease release, CancellationToken cancel)
        {
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) return;

            var internalSettings = RunSynthesisMutagenPatcher.Factory(settings);
            internalSettings.ExtraDataFolder = _ExtraDataPathProvider.Path;

            var args = Parser.Default.FormatCommandLine(internalSettings);
            try
            {
                using var process = _ProcessFactory.Create(
                    new ProcessStartInfo(ExePath.Path, args)
                    {
                        WorkingDirectory = Path.GetDirectoryName(ExePath.Path)!
                    },
                    cancel);
                using var outputSub = process.Output.Subscribe(_output);
                using var errSub = process.Error.Subscribe(_error);
                var result = await process.Run();
                if (result != 0)
                {
                    throw new CliUnsuccessfulRunException(
                        result,
                        $"Process exited in failure: {process.StartInfo.FileName} {internalSettings}");
                }
            }
            catch (Win32Exception ex)
            {
                throw new FileNotFoundException($"Could not find target CLI file: {ExePath.Path}", ex);
            }
        }
    }
}
