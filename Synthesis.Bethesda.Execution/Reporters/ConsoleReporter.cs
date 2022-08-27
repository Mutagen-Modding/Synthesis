using Noggog;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Reporters;

[ExcludeFromCodeCoverage]
public class ConsoleReporter : IRunReporter
{
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

    public void ReportRunProblem(Guid key, string name, Exception? ex)
    {
        if (ex == null)
        {
            System.Console.Error.WriteLine($"[{name}] Run error");
        }
        else
        {
            System.Console.Error.WriteLine($"[{name}] Run error:");
            System.Console.Error.WriteLine(ex);
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