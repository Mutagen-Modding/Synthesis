using System;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public interface IRunReporter
    {
        void ReportOverallProblem(Exception ex);
        void WriteOverall(string str);
        void WriteErrorOverall(string str);
        void ReportPrepProblem(Guid key, string name, Exception ex);
        void ReportRunProblem(Guid key, string name, Exception ex);
        void ReportStartingRun(Guid key, string name);
        void ReportRunSuccessful(Guid key, string name, string outputPath);
        void Write(Guid key, string? name, string str);
        void WriteError(Guid key, string? name, string str);
    }
}
