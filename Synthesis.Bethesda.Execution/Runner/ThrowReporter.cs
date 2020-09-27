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

        public void ReportError(string str)
        {
        }

        public void ReportOutput(string str)
        {
        }

        public void ReportOutputMapping(IPatcherRun patcher, string str)
        {
        }

        public void ReportOverallProblem(Exception ex)
        {
            throw ex;
        }

        public void ReportPrepProblem(IPatcherRun patcher, Exception ex)
        {
            throw ex;
        }

        public void ReportRunProblem(IPatcherRun patcher, Exception ex)
        {
            throw ex;
        }

        public void ReportRunSuccessful(IPatcherRun patcher, string outputPath)
        {
        }

        public void ReportStartingRun(IPatcherRun patcher)
        {
        }
    }
}
