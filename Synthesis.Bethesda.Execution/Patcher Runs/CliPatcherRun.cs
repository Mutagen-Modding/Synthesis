using Mutagen.Bethesda;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.Execution
{
    public class CliPatcherRun : IPatcherRun
    {
        public string Name { get; }

        private readonly Subject<string> _output = new Subject<string>();
        public IObservable<string> Output => _output;

        private readonly Subject<string> _error = new Subject<string>();
        public IObservable<string> Error => _error;

        public string PathToExecutable;

        public CliPatcherRun(string pathToExecutable)
        {
            Name = Path.GetFileName(pathToExecutable);
            PathToExecutable = pathToExecutable;
        }

        public void Dispose()
        {
        }

        public async Task Prep(GameRelease release, CancellationToken? cancel = null)
        {
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
        {
            if (cancel?.IsCancellationRequested ?? false) return;
            cancel ??= CancellationToken.None;
            try
            {
                TaskCompletionSource completeTask = new TaskCompletionSource();
                var args = Parser.Default.FormatCommandLine(settings);
                var process = new Process();
                process.EnableRaisingEvents = true;
                CancellationTokenRegistration? cancelSub;
                // Register process kill in a paranoid way
                try
                {
                    cancelSub = cancel.Value.Register(() =>
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    });
                }
                catch (ObjectDisposedException)
                { // Cancellation happened in between our checks?
                    return;
                }
                process.Exited += (s, e) =>
                {
                    if (process.ExitCode != 0 && !cancel.Value.IsCancellationRequested)
                    {
                        completeTask.SetException(
                            new CliUnsuccessfulRunException(
                                process.ExitCode,
                                $"Process exited in failure: {process.StartInfo.FileName} {process.StartInfo.Arguments}"));
                    }
                    else
                    {
                        completeTask.Complete();
                    }
                };
                process.StartInfo = new ProcessStartInfo(PathToExecutable, args)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };

                // Latch on and read output
                process.OutputDataReceived += (_, data) =>
                {
                    _output.OnNext(data.Data);
                };
                process.ErrorDataReceived += (_, data) =>
                {
                    _error.OnNext(data.Data);
                };

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                await completeTask.Task;
                cancelSub?.Dispose();
            }
            catch (Win32Exception ex)
            {
                throw new FileNotFoundException($"Could not find target CLI file: {PathToExecutable}", ex);
            }
        }
    }
}
