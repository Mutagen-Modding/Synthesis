using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Runner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class RunningPatchersVM : ViewModel, IRunReporter
    {
        public ProfileVM RunningProfile { get; }

        private CancellationTokenSource _cancel = new CancellationTokenSource();

        [Reactive]
        public Exception? ResultError { get; private set; }

        [Reactive]
        public bool Running { get; private set; } = true;

        public RunningPatchersVM(ProfileVM profile)
        {
            RunningProfile = profile;
        }

        public async Task Run()
        {
            try
            {
                var output = Path.Combine(RunningProfile.WorkingDirectory, Synthesis.Bethesda.Constants.SynthesisModKey.FileName);
                await Runner.Run(
                    workingDirectory: RunningProfile.WorkingDirectory,
                    outputPath: output,
                    dataFolder: RunningProfile.DataFolder,
                    release: RunningProfile.Release,
                    loadOrder: RunningProfile.LoadOrder.Items,
                    cancellation: _cancel.Token,
                    reporter: this,
                    patchers: RunningProfile.Patchers.Items
                        .Where(x => x.IsOn)
                        .Select(p => p.ToRunner()));
                var dataFolderPath = Path.Combine(RunningProfile.DataFolder, Synthesis.Bethesda.Constants.SynthesisModKey.FileName);
                File.Copy(output, dataFolderPath, overwrite: true);
            }
            catch (Exception ex)
            {
                ResultError = ex;
            }
            finally
            {
                Running = false;
            }
        }

        public void ReportOverallProblem(Exception ex)
        {
            throw new NotImplementedException();
        }

        public void ReportPrepProblem(IPatcherRun patcher, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void ReportRunProblem(IPatcherRun patcher, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void ReportOutputMapping(IPatcherRun patcher, string str)
        {
        }
    }
}
