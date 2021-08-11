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
            throw ex;
        }

        public void ReportRunProblem(Guid key, string name, Exception ex)
        {
            throw ex;
        }

        public void ReportStartingRun(Guid key, string name)
        {
        }

        public void ReportRunSuccessful(Guid key, string name, string outputPath)
        {
        }

        public void Write(Guid key, string? name, string str)
        {
        }

        public void WriteError(Guid key, string? name, string str)
        {
        }
    }
}
