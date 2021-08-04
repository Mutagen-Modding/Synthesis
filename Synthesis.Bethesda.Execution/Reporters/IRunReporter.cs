using System;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public interface IRunReporter
    {
        void ReportOverallProblem(Exception ex);
        void WriteOverall(string str);
        void WriteErrorOverall(string str);
        void ReportPrepProblem(int key, string name, Exception ex);
        void ReportRunProblem(int key, string name, Exception ex);
        void ReportStartingRun(int key, string name);
        void ReportRunSuccessful(int key, string name, string outputPath);
        void Write(int key, string? name, string str);
        void WriteError(int key, string? name, string str);
    }
}
