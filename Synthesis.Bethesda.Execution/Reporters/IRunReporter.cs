using System;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public interface IRunReporter : IRunReporter<object?>
    {
    }

    public interface IRunReporter<TKey>
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(TKey key, string name, Exception ex);
        void ReportRunProblem(TKey key, string name, Exception ex);
        void ReportStartingRun(TKey key, string name);
        void ReportRunSuccessful(TKey key, string name, string outputPath);
        void Write(TKey key, string? name, string str);
        void WriteError(TKey key, string? name, string str);
    }
}
