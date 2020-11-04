using Noggog;
using Synthesis.Bethesda.Execution.Patchers;
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

        public void ReportPrepProblem(IPatcherRun patcher, Exception ex)
        {
            System.Console.Error.WriteLine($"[{patcher.Name}] Preparation error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunProblem(IPatcherRun patcher, Exception ex)
        {
            System.Console.Error.WriteLine($"[{patcher.Name}] Run error:");
            System.Console.Error.WriteLine(ex);
        }

        public void ReportRunSuccessful(IPatcherRun patcher, string outputPath)
        {
            System.Console.WriteLine($"[{patcher.Name}] Run successful.");
        }

        public void ReportStartingRun(IPatcherRun patcher)
        {
            System.Console.WriteLine($"[{patcher.Name}] Starting run.");
        }

        public void Write(IPatcherRun? patcher, string str)
        {
            System.Console.WriteLine($"{patcher?.Name.Decorate(x => $"[{x}] ")}{str}");
        }

        public void WriteError(IPatcherRun? patcher, string str)
        {
            System.Console.Error.WriteLine($"{patcher?.Name.Decorate(x => $"[{x}] ")}{str}");
        }
    }
}
