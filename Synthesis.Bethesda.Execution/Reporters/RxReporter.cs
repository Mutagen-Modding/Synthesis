using System;
using System.Reactive.Subjects;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public class RxReporter<TKey> : IRunReporter<TKey>
    {
        private readonly Subject<Exception> _overall = new();
        private readonly Subject<(TKey, string, Exception)> _prepProblem = new();
        private readonly Subject<(TKey, string, Exception)> _runProblem = new();
        private readonly Subject<(TKey, string, string)> _runSuccessful = new();
        private readonly Subject<(TKey, string)> _starting = new();
        private readonly Subject<(TKey Key, string? Run, string String)> _output = new();
        private readonly Subject<(TKey Key, string? Run, string String)> _error = new();

        public IObservable<Exception> Overall => _overall;
        public IObservable<(TKey Key, string Run, Exception Error)> PrepProblem => _prepProblem;
        public IObservable<(TKey Key, string Run, Exception Error)> RunProblem => _runProblem;
        public IObservable<(TKey Key, string Run, string OutputPath)> RunSuccessful => _runSuccessful;
        public IObservable<(TKey Key, string Run)> Starting => _starting;
        public IObservable<(TKey Key, string? Run, string String)> Output => _output;
        public IObservable<(TKey Key, string? Run, string String)> Error => _error;

        public void WriteError(TKey key, string? name, string str)
        {
            _error.OnNext((key, name, str));
        }

        public void Write(TKey key, string? name, string str)
        {
            _output.OnNext((key, name, str));
        }

        public void ReportOverallProblem(Exception ex)
        {
            _overall.OnNext(ex);
        }

        public void ReportPrepProblem(TKey key, string name, Exception ex)
        {
            _prepProblem.OnNext((key, name, ex));
        }

        public void ReportRunProblem(TKey key, string name, Exception ex)
        {
            _runProblem.OnNext((key, name, ex));
        }

        public void ReportRunSuccessful(TKey key, string name, string outputPath)
        {
            _runSuccessful.OnNext((key, name, outputPath));
        }

        public void ReportStartingRun(TKey key, string name)
        {
            _starting.OnNext((key, name));
        }
    }
}
