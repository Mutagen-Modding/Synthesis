using System;
using Synthesis.Bethesda.Execution.Patchers.Running;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public interface IRunReporter : IRunReporter<object?>
    {
    }

    public interface IRunReporter<TKey>
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(TKey key, IPatcherRun patcher, Exception ex);
        void ReportRunProblem(TKey key, IPatcherRun patcher, Exception ex);
        void ReportStartingRun(TKey key, IPatcherRun patcher);
        void ReportRunSuccessful(TKey key, IPatcherRun patcher, string outputPath);
        void Write(TKey key, IPatcherRun? patcher, string str);
        void WriteError(TKey key, IPatcherRun? patcher, string str);
    }
}
