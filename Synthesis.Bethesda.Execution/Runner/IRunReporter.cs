using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Runner
{
    public interface IRunReporter
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(IPatcher patcher, Exception ex);
        void ReportRunProblem(IPatcher patcher, Exception ex);
        void ReportOutputMapping(IPatcher patcher, string str);
    }
}
