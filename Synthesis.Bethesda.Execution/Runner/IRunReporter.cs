using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Runner
{
    public interface IRunReporter
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(IPatcherRun patcher, Exception ex);
        void ReportRunProblem(IPatcherRun patcher, Exception ex);
        void ReportOutputMapping(IPatcherRun patcher, string str);
    }
}
