using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
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
        private readonly IPatcherNameProvider _NameProvider;
        private readonly IPatcherIdProvider _IdProvider;
        private readonly IRunReporter _Reporter;
        private readonly Subject<LogEvent> _events = new();
        public IObservable<LogEvent> Events => _events;

        public ReporterLoggerWrapper(
            IPatcherNameProvider nameProvider,
            IPatcherIdProvider idProvider,
            IRunReporter reporter)
        {
            _NameProvider = nameProvider;
            _IdProvider = idProvider;
            _Reporter = reporter;
        }

        public void Write(LogEvent logEvent)
        {
            _events.OnNext(logEvent);
            switch (logEvent.Level)
            {
                case LogEventLevel.Error:
                case LogEventLevel.Fatal:
                    _Reporter.WriteError(_IdProvider.InternalId, _NameProvider.Name, logEvent.RenderMessage());
                    break;
                default:
                    _Reporter.Write(_IdProvider.InternalId, _NameProvider.Name, logEvent.RenderMessage());
                    break;
            }
        }
    }
}