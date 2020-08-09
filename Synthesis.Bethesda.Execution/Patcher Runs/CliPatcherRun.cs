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

namespace Synthesis.Bethesda.Execution
{
    public class CliPatcherRun : IPatcherRun
    {
        public string Name { get; }

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
            try
            {
                TaskCompletionSource completeTask = new TaskCompletionSource();
                var args = Parser.Default.FormatCommandLine(settings);
                var process = new Process();
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) =>
                {
                    if (process.ExitCode != 0)
                    {
                        completeTask.SetException(
                            new CliUnsuccessfulRunException(
                                process.ExitCode,
                                $"Process exited in failure: {process.StartInfo.FileName} {process.StartInfo.Arguments}"));
                    }
                    completeTask.Complete();
                };
                process.StartInfo = new ProcessStartInfo(PathToExecutable, args)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                };
                process.Start();
                // Register process kill in a paranoid way
                try
                {
                    using var disp = cancel?.Register(() =>
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
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
                await completeTask.Task;
            }
            catch (Win32Exception ex)
            {
                throw new FileNotFoundException($"Could not find target CLI file: {PathToExecutable}", ex);
            }
        }
    }
}
