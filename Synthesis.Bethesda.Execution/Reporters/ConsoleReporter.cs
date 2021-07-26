using Noggog;
using System;
using Synthesis.Bethesda.Execution.Patchers.Running;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public class ConsoleReporter : IRunReporter<object?>
    {
        public void ReportOverallProblem(Exception ex)
        {
            System.Console.Error.WriteLine("Overall error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportPrepProblem(object? key, IPatcherRun patcher, Exception ex)
        {
            System.Console.Error.WriteLine($"[{patcher.Name}] Preparation error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunProblem(object? key, IPatcherRun patcher, Exception ex)
        {
            System.Console.Error.WriteLine($"[{patcher.Name}] Run error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunSuccessful(object? key, IPatcherRun patcher, string outputPath)
        {
            System.Console.WriteLine($"[{patcher.Name}] Run successful.");
        }

        public void ReportStartingRun(object? key, IPatcherRun patcher)
        {
            System.Console.WriteLine($"[{patcher.Name}] Starting run.");
        }

        public void Write(object? key, IPatcherRun? patcher, string str)
        {
            System.Console.WriteLine($"{patcher?.Name.Decorate(x => $"[{x}] ")}{str}");
        }

        public void WriteError(object? key, IPatcherRun? patcher, string str)
        {
            System.Console.Error.WriteLine($"{patcher?.Name.Decorate(x => $"[{x}] ")}{str}");
        }
    }
}
