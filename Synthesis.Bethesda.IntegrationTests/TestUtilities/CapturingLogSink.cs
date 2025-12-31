using Serilog.Core;
using Serilog.Events;

namespace Synthesis.Bethesda.IntegrationTests.TestUtilities;

/// <summary>
/// A Serilog sink that captures log messages for later inspection in tests
/// </summary>
public class CapturingLogSink : ILogEventSink
{
    private readonly List<LogEvent> _events = new();
    private readonly object _lock = new();

    public void Emit(LogEvent logEvent)
    {
        lock (_lock)
        {
            _events.Add(logEvent);
        }
    }

    /// <summary>
    /// Gets all captured log events
    /// </summary>
    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_lock)
            {
                return _events.ToList();
            }
        }
    }

    /// <summary>
    /// Gets all captured log messages as strings
    /// </summary>
    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_lock)
            {
                return _events
                    .Select(e => e.RenderMessage())
                    .ToList();
            }
        }
    }

    /// <summary>
    /// Gets all ERROR level messages
    /// </summary>
    public IReadOnlyList<string> ErrorMessages
    {
        get
        {
            lock (_lock)
            {
                return _events
                    .Where(e => e.Level == LogEventLevel.Error)
                    .Select(e => e.RenderMessage())
                    .ToList();
            }
        }
    }

    /// <summary>
    /// Gets the full text of all captured logs (including rendered exceptions)
    /// </summary>
    public string GetFullLog()
    {
        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var evt in _events)
            {
                sb.Append($"[{evt.Level}] {evt.RenderMessage()}");
                if (evt.Exception != null)
                {
                    sb.AppendLine();
                    sb.Append(evt.Exception.ToString());
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
