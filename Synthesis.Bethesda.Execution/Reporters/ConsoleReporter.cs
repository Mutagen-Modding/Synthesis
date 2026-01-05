using Noggog;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using Synthesis.Bethesda.Execution.Exceptions;
using Mutagen.Bethesda.Plugins.Order;

namespace Synthesis.Bethesda.Execution.Reporters;

[ExcludeFromCodeCoverage]
public class ConsoleReporter : IRunReporter
{
    private readonly IErrorClassifier _errorClassifier;
    private readonly ILogger _logger;

    public ConsoleReporter(IErrorClassifier errorClassifier, ILogger logger)
    {
        _errorClassifier = errorClassifier;
        _logger = logger;
    }

    public void ReportOverallProblem(Exception ex)
    {
        System.Console.Error.WriteLine("Overall error:");
        System.Console.Error.WriteLine(ex);
    }

    public void WriteOverall(string str)
    {
        Write(default, default, str);
    }

    public void WriteErrorOverall(string str)
    {
        WriteError(default, default, str);
    }

    public void ReportPrepProblem(Guid key, string name, Exception ex)
    {
        System.Console.Error.WriteLine($"[{name}] Preparation error:");
        System.Console.Error.WriteLine(ex);
    }

    public void ReportRunProblem(
        Guid key,
        string name,
        Exception? ex,
        IReadOnlyList<string>? capturedOutput = null,
        IReadOnlyList<string>? capturedErrors = null,
        IList<ILoadOrderListingGetter>? loadOrder = null)
    {
        // Attempt to classify the error and provide helpful suggestions
        var classification = _errorClassifier.Classify(capturedOutput, capturedErrors, loadOrder);
        if (classification != null)
        {
            if (ex != null)
            {
                _logger.Error(ex, $"[{name}] Run error:");
            }

            // Also log via ILogger so tests can capture it
            _logger.Error("Error detected: {ErrorType}", classification.ErrorType);
            _logger.Error("{Message}", classification.Message);

            throw new ClassifiedErrorException(ex);
        }
    }

    public void ReportRunSuccessful(Guid key, string name, string outputPath)
    {
        System.Console.WriteLine($"[{name}] Run successful.");
    }

    public void ReportStartingRun(Guid key, string name)
    {
        System.Console.WriteLine($"[{name}] Starting run.");
    }

    public void Write(Guid key, string? name, string str)
    {
        System.Console.WriteLine($"{name?.Decorate(x => $"[{x}] ")}{str}");
    }

    public void WriteError(Guid key, string? name, string str)
    {
        System.Console.Error.WriteLine($"{name?.Decorate(x => $"[{x}] ")}{str}");
    }
}