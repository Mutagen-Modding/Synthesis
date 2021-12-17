using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public interface IRunReporterWatcher
    {       
        public IObservable<Exception> Exceptions { get; }
        public IObservable<(Guid Key, string Run, Exception Error)> PrepProblem { get; }
        public IObservable<(Guid Key, string Run, Exception Error)> RunProblem { get; }
        public IObservable<(Guid Key, string Run, string OutputPath)> RunSuccessful { get; }
        public IObservable<(Guid Key, string Run)> Starting { get; }
        public IObservable<(Guid Key, string? Run, string String)> Output { get; }
        public IObservable<(Guid Key, string? Run, string String)> Error { get; }
    }
    
    [ExcludeFromCodeCoverage]
    public class RxReporter : IRunReporter, IRunReporterWatcher
    {
        private readonly Subject<Exception> _overall = new();
        private readonly Subject<(Guid, string, Exception)> _prepProblem = new();
        private readonly Subject<(Guid, string, Exception)> _runProblem = new();
        private readonly Subject<(Guid, string, string)> _runSuccessful = new();
        private readonly Subject<(Guid, string)> _starting = new();
        private readonly Subject<(Guid Key, string? Run, string String)> _output = new();
        private readonly Subject<(Guid Key, string? Run, string String)> _error = new();

        public IObservable<Exception> Exceptions => _overall;
        public IObservable<(Guid Key, string Run, Exception Error)> PrepProblem => _prepProblem;
        public IObservable<(Guid Key, string Run, Exception Error)> RunProblem => _runProblem;
        public IObservable<(Guid Key, string Run, string OutputPath)> RunSuccessful => _runSuccessful;
        public IObservable<(Guid Key, string Run)> Starting => _starting;
        public IObservable<(Guid Key, string? Run, string String)> Output => _output;
        public IObservable<(Guid Key, string? Run, string String)> Error => _error;

        public void WriteError(Guid key, string? name, string str)
        {
            _error.OnNext((key, name, str));
        }

        public void Write(Guid key, string? name, string str)
        {
            _output.OnNext((key, name, str));
        }

        public void ReportOverallProblem(Exception ex)
        {
            _overall.OnNext(ex);
        }

        public void ReportPrepProblem(Guid key, string name, Exception ex)
        {
            _prepProblem.OnNext((key, name, ex));
        }

        public void ReportRunProblem(Guid key, string name, Exception ex)
        {
            _runProblem.OnNext((key, name, ex));
        }

        public void ReportRunSuccessful(Guid key, string name, string outputPath)
        {
            _runSuccessful.OnNext((key, name, outputPath));
        }

        public void ReportStartingRun(Guid key, string name)
        {
            _starting.OnNext((key, name));
        }

        public void WriteOverall(string str)
        {
            Write(default, default, str);
        }

        public void WriteErrorOverall(string str)
        {
            WriteError(default, default, str);
        }
    }
}
