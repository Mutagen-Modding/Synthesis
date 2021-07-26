using System;
using Synthesis.Bethesda.Execution.Patchers.Running;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public class ThrowReporter : IRunReporter
    {
        public static ThrowReporter Instance = new();

        private ThrowReporter()
        {
        }

        public void ReportOverallProblem(Exception ex)
        {
            throw ex;
        }

        public void ReportPrepProblem(object? key, IPatcherRun patcher, Exception ex)
        {
            throw ex;
        }

        public void ReportRunProblem(object? key, IPatcherRun patcher, Exception ex)
        {
            throw ex;
        }

        public void ReportStartingRun(object? key, IPatcherRun patcher)
        {
        }

        public void ReportRunSuccessful(object? key, IPatcherRun patcher, string outputPath)
        {
        }

        public void Write(object? key, IPatcherRun? patcher, string str)
        {
        }

        public void WriteError(object? key, IPatcherRun? patcher, string str)
        {
        }
    }
}
