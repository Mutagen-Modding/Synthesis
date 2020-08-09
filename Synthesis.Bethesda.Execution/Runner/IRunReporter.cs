using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Runner
{
    public interface IRunReporter<TKey>
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(TKey key, IPatcherRun patcher, Exception ex);
        void ReportRunProblem(TKey key, IPatcherRun patcher, Exception ex);
        void ReportStartingRun(TKey key, IPatcherRun patcher);
        void ReportRunSuccessful(TKey key, IPatcherRun patcher, string outputPath);
    }

    public interface IRunReporter
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(IPatcherRun patcher, Exception ex);
        void ReportRunProblem(IPatcherRun patcher, Exception ex);
        void ReportStartingRun(IPatcherRun patcher);
        void ReportRunSuccessful(IPatcherRun patcher, string outputPath);
    }
}
