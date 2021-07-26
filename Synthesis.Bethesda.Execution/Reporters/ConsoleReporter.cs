using Noggog;
using System;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public class ConsoleReporter : IRunReporter
    {
        public void ReportOverallProblem(Exception ex)
        {
            System.Console.Error.WriteLine("Overall error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportPrepProblem(object? key, string name, Exception ex)
        {
            System.Console.Error.WriteLine($"[{name}] Preparation error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunProblem(object? key, string name, Exception ex)
        {
            System.Console.Error.WriteLine($"[{name}] Run error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunSuccessful(object? key, string name, string outputPath)
        {
            System.Console.WriteLine($"[{name}] Run successful.");
        }

        public void ReportStartingRun(object? key, string name)
        {
            System.Console.WriteLine($"[{name}] Starting run.");
        }

        public void Write(object? key, string? name, string str)
        {
            System.Console.WriteLine($"{name?.Decorate(x => $"[{x}] ")}{str}");
        }

        public void WriteError(object? key, string? name, string str)
        {
            System.Console.Error.WriteLine($"{name?.Decorate(x => $"[{x}] ")}{str}");
        }
    }
}
