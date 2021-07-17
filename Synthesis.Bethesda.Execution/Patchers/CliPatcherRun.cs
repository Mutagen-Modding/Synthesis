using Mutagen.Bethesda;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using CommandLine;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Noggog.Utility;
using Mutagen.Bethesda.Synthesis.CLI;

namespace Synthesis.Bethesda.Execution.Patchers
{
    public interface ICliPatcherRun : IPatcherRun
    {
    }

    public class CliPatcherRun : ICliPatcherRun
    {
        private readonly CompositeDisposable _disposable = new();
        
        public string Name { get; }

        private readonly Subject<string> _output = new();
        public IObservable<string> Output => _output;

        private readonly Subject<string> _error = new();
        public IObservable<string> Error => _error;

        private readonly IProcessFactory _ProcessFactory;
        public string PathToExecutable;

        public string? PathToExtraData;

        public delegate ICliPatcherRun Factory(string nickname, string pathToExecutable, string? pathToExtra);

        public CliPatcherRun(
            IProcessFactory processFactory,
            string nickname,
            string pathToExecutable, 
            string? pathToExtra)
        {
            Name = nickname;
            _ProcessFactory = processFactory;
            PathToExecutable = pathToExecutable;
            PathToExtraData = pathToExtra;
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
            internalSettings.ExtraDataFolder = PathToExtraData;

            var args = Parser.Default.FormatCommandLine(internalSettings);
            try
            {
                using var process = _ProcessFactory.Create(
                    new ProcessStartInfo(PathToExecutable, args)
                    {
                        WorkingDirectory = Path.GetDirectoryName(PathToExecutable)!
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
                throw new FileNotFoundException($"Could not find target CLI file: {PathToExecutable}", ex);
            }
        }
    }
}
