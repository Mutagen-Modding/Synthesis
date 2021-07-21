using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using Synthesis.Bethesda.Execution.Patchers.Running;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public class RxReporter<TKey> : IRunReporter<TKey>
    {
        private readonly Subject<Exception> _overall = new();
        private readonly Subject<(TKey, IPatcherRun, Exception)> _prepProblem = new();
        private readonly Subject<(TKey, IPatcherRun, Exception)> _runProblem = new();
        private readonly Subject<(TKey, IPatcherRun, string)> _runSuccessful = new();
        private readonly Subject<(TKey, IPatcherRun)> _starting = new();
        private readonly Subject<(TKey Key, IPatcherRun? Run, string String)> _output = new();
        private readonly Subject<(TKey Key, IPatcherRun? Run, string String)> _error = new();

        public IObservable<Exception> Overall => _overall;
        public IObservable<(TKey Key, IPatcherRun Run, Exception Error)> PrepProblem => _prepProblem;
        public IObservable<(TKey Key, IPatcherRun Run, Exception Error)> RunProblem => _runProblem;
        public IObservable<(TKey Key, IPatcherRun Run, string OutputPath)> RunSuccessful => _runSuccessful;
        public IObservable<(TKey Key, IPatcherRun Run)> Starting => _starting;
        public IObservable<(TKey Key, IPatcherRun? Run, string String)> Output => _output;
        public IObservable<(TKey Key, IPatcherRun? Run, string String)> Error => _error;

        public void WriteError(TKey key, IPatcherRun? patcher, string str)
        {
            _error.OnNext((key, patcher, str));
        }

        public void Write(TKey key, IPatcherRun? patcher, string str)
        {
            _output.OnNext((key, patcher, str));
        }

        public void ReportOverallProblem(Exception ex)
        {
            _overall.OnNext(ex);
        }

        public void ReportPrepProblem(TKey key, IPatcherRun patcher, Exception ex)
        {
            _prepProblem.OnNext((key, patcher, ex));
        }

        public void ReportRunProblem(TKey key, IPatcherRun patcher, Exception ex)
        {
            _runProblem.OnNext((key, patcher, ex));
        }

        public void ReportRunSuccessful(TKey key, IPatcherRun patcher, string outputPath)
        {
            _runSuccessful.OnNext((key, patcher, outputPath));
        }

        public void ReportStartingRun(TKey key, IPatcherRun patcher)
        {
            _starting.OnNext((key, patcher));
        }
    }
}
