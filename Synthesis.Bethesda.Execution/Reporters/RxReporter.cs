using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;

namespace Synthesis.Bethesda.Execution.Reporters
{
    [ExcludeFromCodeCoverage]
    public class RxReporter : IRunReporter
    {
        private readonly Subject<Exception> _overall = new();
        private readonly Subject<(int, string, Exception)> _prepProblem = new();
        private readonly Subject<(int, string, Exception)> _runProblem = new();
        private readonly Subject<(int, string, string)> _runSuccessful = new();
        private readonly Subject<(int, string)> _starting = new();
        private readonly Subject<(int Key, string? Run, string String)> _output = new();
        private readonly Subject<(int Key, string? Run, string String)> _error = new();

        public IObservable<Exception> Overall => _overall;
        public IObservable<(int Key, string Run, Exception Error)> PrepProblem => _prepProblem;
        public IObservable<(int Key, string Run, Exception Error)> RunProblem => _runProblem;
        public IObservable<(int Key, string Run, string OutputPath)> RunSuccessful => _runSuccessful;
        public IObservable<(int Key, string Run)> Starting => _starting;
        public IObservable<(int Key, string? Run, string String)> Output => _output;
        public IObservable<(int Key, string? Run, string String)> Error => _error;

        public void WriteError(int key, string? name, string str)
        {
            _error.OnNext((key, name, str));
        }

        public void Write(int key, string? name, string str)
        {
            _output.OnNext((key, name, str));
        }

        public void ReportOverallProblem(Exception ex)
        {
            _overall.OnNext(ex);
        }

        public void ReportPrepProblem(int key, string name, Exception ex)
        {
            _prepProblem.OnNext((key, name, ex));
        }

        public void ReportRunProblem(int key, string name, Exception ex)
        {
            _runProblem.OnNext((key, name, ex));
        }

        public void ReportRunSuccessful(int key, string name, string outputPath)
        {
            _runSuccessful.OnNext((key, name, outputPath));
        }

        public void ReportStartingRun(int key, string name)
        {
            _starting.OnNext((key, name));
        }
    }
}
