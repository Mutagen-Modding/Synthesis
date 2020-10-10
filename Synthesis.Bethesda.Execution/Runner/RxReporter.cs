using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace Synthesis.Bethesda.Execution.Runner
{
    public class RxReporter<TKey> : IRunReporter<TKey>
    {
        private readonly Subject<Exception> _overall = new Subject<Exception>();
        private readonly Subject<(TKey, IPatcherRun, Exception)> _prepProblem = new Subject<(TKey, IPatcherRun, Exception)>();
        private readonly Subject<(TKey, IPatcherRun, Exception)> _runProblem = new Subject<(TKey, IPatcherRun, Exception)>();
        private readonly Subject<(TKey, IPatcherRun, string)> _runSuccessful = new Subject<(TKey, IPatcherRun, string)>();
        private readonly Subject<(TKey, IPatcherRun)> _starting = new Subject<(TKey, IPatcherRun)>();
        private readonly Subject<string> _output = new Subject<string>();
        private readonly Subject<string> _error = new Subject<string>();

        public IObservable<Exception> Overall => _overall;
        public IObservable<(TKey Key, IPatcherRun Run, Exception Error)> PrepProblem => _prepProblem;
        public IObservable<(TKey Key, IPatcherRun Run, Exception Error)> RunProblem => _runProblem;
        public IObservable<(TKey Key, IPatcherRun Run, string OutputPath)> RunSuccessful => _runSuccessful;
        public IObservable<(TKey Key, IPatcherRun Run)> Starting => _starting;
        public IObservable<string> Output => _output;
        public IObservable<string> Error => _error;

        public void WriteError(string str)
        {
            _error.OnNext(str);
        }

        public void Write(string str)
        {
            _output.OnNext(str);
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
