using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;
using Synthesis.Bethesda.Execution.Patchers.Running;

namespace Synthesis.Bethesda.Execution.Reporters
{
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

    public interface IRunReporter
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(IPatcherRun patcher, Exception ex);
        void ReportRunProblem(IPatcherRun patcher, Exception ex);
        void ReportStartingRun(IPatcherRun patcher);
        void ReportRunSuccessful(IPatcherRun patcher, string outputPath);
        void Write(IPatcherRun? patcher, string str);
        void WriteError(IPatcherRun? patcher, string str);
    }
}
