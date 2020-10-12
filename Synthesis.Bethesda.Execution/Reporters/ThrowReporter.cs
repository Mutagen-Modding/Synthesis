using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public class ThrowReporter : IRunReporter
    {
        public static ThrowReporter Instance = new ThrowReporter();

        private ThrowReporter()
        {
        }

        public void WriteError(IPatcherRun? patcher, string str)
        {
        }

        public void Write(IPatcherRun? patcher, string str)
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
