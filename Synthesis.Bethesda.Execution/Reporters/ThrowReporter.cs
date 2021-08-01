using System;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Reporters
{
    [ExcludeFromCodeCoverage]
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

        public void ReportPrepProblem(int key, string name, Exception ex)
        {
            throw ex;
        }

        public void ReportRunProblem(int key, string name, Exception ex)
        {
            throw ex;
        }

        public void ReportStartingRun(int key, string name)
        {
        }

        public void ReportRunSuccessful(int key, string name, string outputPath)
        {
        }

        public void Write(int key, string? name, string str)
        {
        }

        public void WriteError(int key, string? name, string str)
        {
        }
    }
}
