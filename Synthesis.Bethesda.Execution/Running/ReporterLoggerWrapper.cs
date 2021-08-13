using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running
{
    public interface IReporterLoggerWrapper
    {
        IObservable<LogEvent> Events { get; }
    }

    [ExcludeFromCodeCoverage]
    public class ReporterLoggerWrapper : ILogger, IReporterLoggerWrapper
    {
        private readonly IPatcherNameProvider _nameProvider;
        private readonly IPatcherIdProvider _idProvider;
        private readonly IRunReporter _reporter;
        private readonly Subject<LogEvent> _events = new();
        public IObservable<LogEvent> Events => _events;

        public ReporterLoggerWrapper(
            IPatcherNameProvider nameProvider,
            IPatcherIdProvider idProvider,
            IRunReporter reporter)
        {
            _nameProvider = nameProvider;
            _idProvider = idProvider;
            _reporter = reporter;
        }

        public void Write(LogEvent logEvent)
        {
            _events.OnNext(logEvent);
            switch (logEvent.Level)
            {
                case LogEventLevel.Error:
                case LogEventLevel.Fatal:
                    _reporter.WriteError(_idProvider.InternalId, _nameProvider.Name, logEvent.RenderMessage());
                    break;
                default:
                    _reporter.Write(_idProvider.InternalId, _nameProvider.Name, logEvent.RenderMessage());
                    break;
            }
        }
    }
}