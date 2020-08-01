using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Runner
{
    public class ThrowReporter : IRunReporter
    {
        public static ThrowReporter Instance = new ThrowReporter();

        private ThrowReporter()
        {
        }

        public void ReportOutputMapping(IPatcher patcher, string str)
        {
        }

        public void ReportOverallProblem(Exception ex)
        {
            throw ex;
        }

        public void ReportPrepProblem(IPatcher patcher, Exception ex)
        {
            throw ex;
        }

        public void ReportRunProblem(IPatcher patcher, Exception ex)
        {
            throw ex;
        }
    }
}
