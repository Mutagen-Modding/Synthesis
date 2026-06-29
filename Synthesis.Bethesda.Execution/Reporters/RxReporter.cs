using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using Mutagen.Bethesda.Plugins.Order;

namespace Synthesis.Bethesda.Execution.Reporters;

public interface IRunReporterWatcher
{
    public IObservable<Exception> Exceptions { get; }
    public IObservable<(Guid Key, string Run, Exception Error, ErrorClassification? Classification)> PrepProblem { get; }
    public IObservable<(Guid Key, string Run, Exception? Error, ErrorClassification? Classification)> RunProblem { get; }
    public IObservable<(Guid Key, string Run, string OutputPath)> RunSuccessful { get; }
    public IObservable<(Guid Key, string Run)> Starting { get; }
    public IObservable<(Guid Key, string? Run, string String)> Output { get; }
    public IObservable<(Guid Key, string? Run, string String)> Error { get; }
}

[ExcludeFromCodeCoverage]
public class RxReporter : IRunReporter, IRunReporterWatcher
{
    private readonly IErrorClassifier _errorClassifier;
    private readonly Subject<Exception> _overall = new();
    private readonly Subject<(Guid, string, Exception, ErrorClassification?)> _prepProblem = new();
    private readonly Subject<(Guid, string, Exception?, ErrorClassification?)> _runProblem = new();
    private readonly Subject<(Guid, string, string)> _runSuccessful = new();
    private readonly Subject<(Guid, string)> _starting = new();
    private readonly Subject<(Guid Key, string? Run, string String)> _output = new();
    private readonly Subject<(Guid Key, string? Run, string String)> _error = new();

    public IObservable<Exception> Exceptions => _overall;
    public IObservable<(Guid Key, string Run, Exception Error, ErrorClassification? Classification)> PrepProblem => _prepProblem;
    public IObservable<(Guid Key, string Run, Exception? Error, ErrorClassification? Classification)> RunProblem => _runProblem;
    public IObservable<(Guid Key, string Run, string OutputPath)> RunSuccessful => _runSuccessful;
    public IObservable<(Guid Key, string Run)> Starting => _starting;
    public IObservable<(Guid Key, string? Run, string String)> Output => _output;
    public IObservable<(Guid Key, string? Run, string String)> Error => _error;

    public RxReporter(IErrorClassifier errorClassifier)
    {
        _errorClassifier = errorClassifier;
    }

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
        var classification = _errorClassifier.Classify(ex);
        _prepProblem.OnNext((key, name, ex, classification));
    }

    public void ReportRunProblem(
        Guid key,
        string name,
        Exception? ex,
        IReadOnlyList<string>? capturedOutput = null,
        IReadOnlyList<string>? capturedErrors = null,
        IList<ILoadOrderListingGetter>? loadOrder = null)
    {
        // Try captured output first (has more detail), then fall back to exception
        var classification = _errorClassifier.Classify(capturedOutput, capturedErrors, loadOrder);
        if (classification == null && ex != null)
        {
            classification = _errorClassifier.Classify(ex);
        }
        _runProblem.OnNext((key, name, ex, classification));
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