using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Runner
{
    public interface IRunReporter<TKey> : ILogger
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(TKey key, IPatcherRun patcher, Exception ex);
        void ReportRunProblem(TKey key, IPatcherRun patcher, Exception ex);
        void ReportStartingRun(TKey key, IPatcherRun patcher);
        void ReportRunSuccessful(TKey key, IPatcherRun patcher, string outputPath);
        void ReportOutput(string str);
        void ReportError(string str);
    }

    public interface IRunReporter : ILogger
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(IPatcherRun patcher, Exception ex);
        void ReportRunProblem(IPatcherRun patcher, Exception ex);
        void ReportStartingRun(IPatcherRun patcher);
        void ReportRunSuccessful(IPatcherRun patcher, string outputPath);
    }
}
