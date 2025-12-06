using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Components;

/// <summary>
/// Serilog sink that writes to xUnit's ITestOutputHelper
/// </summary>
public class TestOutputSink : ILogEventSink
{
    private readonly ITestOutputHelper _output;
    private readonly IFormatProvider? _formatProvider;

    public TestOutputSink(ITestOutputHelper output, IFormatProvider? formatProvider = null)
    {
        _output = output;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(_formatProvider);
        var timestamp = logEvent.Timestamp.ToString("HH:mm:ss.fff");
        var level = logEvent.Level.ToString().ToUpper();

        try
        {
            _output.WriteLine($"[{timestamp}] [{level}] {message}");

            if (logEvent.Exception != null)
            {
                _output.WriteLine($"Exception: {logEvent.Exception}");
            }
        }
        catch
        {
            // ITestOutputHelper can throw if disposed, ignore
        }
    }
}

/// <summary>
/// Extension methods for configuring TestOutputSink
/// </summary>
public static class TestOutputSinkExtensions
{
    public static LoggerConfiguration TestOutput(
        this LoggerSinkConfiguration sinkConfiguration,
        ITestOutputHelper output,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
        IFormatProvider? formatProvider = null)
    {
        return sinkConfiguration.Sink(new TestOutputSink(output, formatProvider), restrictedToMinimumLevel);
    }
}
