using Noggog;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Reporters
{
    [ExcludeFromCodeCoverage]
    public class ConsoleReporter : IRunReporter
    {
        public void ReportOverallProblem(Exception ex)
        {
            System.Console.Error.WriteLine("Overall error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportPrepProblem(int key, string name, Exception ex)
        {
            System.Console.Error.WriteLine($"[{name}] Preparation error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunProblem(int key, string name, Exception ex)
        {
            System.Console.Error.WriteLine($"[{name}] Run error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunSuccessful(int key, string name, string outputPath)
        {
            System.Console.WriteLine($"[{name}] Run successful.");
        }

        public void ReportStartingRun(int key, string name)
        {
            System.Console.WriteLine($"[{name}] Starting run.");
        }

        public void Write(int key, string? name, string str)
        {
            System.Console.WriteLine($"{name?.Decorate(x => $"[{x}] ")}{str}");
        }

        public void WriteError(int key, string? name, string str)
        {
            System.Console.Error.WriteLine($"{name?.Decorate(x => $"[{x}] ")}{str}");
        }
    }
}
