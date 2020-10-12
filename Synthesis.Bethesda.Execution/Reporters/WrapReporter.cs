using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Reporters
{
    public class WrapReporter : IRunReporter, IRunReporter<object?>
    {
        private readonly IRunReporter _wrapped;

        public WrapReporter(IRunReporter wrapped)
        {
            _wrapped = wrapped;
        }

        public void WriteError(object? key, IPatcherRun? patcher, string str)
        {
            _wrapped.WriteError(patcher, str);
        }

        public void Write(object? key, IPatcherRun? patcher, string str)
        {
            _wrapped.Write(patcher, str);
        }

        public void WriteError(IPatcherRun? patcher, string str)
        {
            _wrapped.WriteError(patcher, str);
        }

        public void Write(IPatcherRun? patcher, string str)
        {
            _wrapped.Write(patcher, str);
        }

        public void ReportOverallProblem(Exception ex)
        {
            _wrapped.ReportOverallProblem(ex);
        }

        public void ReportPrepProblem(object? key, IPatcherRun patcher, Exception ex)
        {
            _wrapped.ReportPrepProblem(patcher, ex);
        }

        public void ReportPrepProblem(IPatcherRun patcher, Exception ex)
        {
            _wrapped.ReportPrepProblem(patcher, ex);
        }

        public void ReportRunProblem(object? key, IPatcherRun patcher, Exception ex)
        {
            _wrapped.ReportRunProblem(patcher, ex);
        }

        public void ReportRunProblem(IPatcherRun patcher, Exception ex)
        {
            _wrapped.ReportRunProblem(patcher, ex);
        }

        public void ReportRunSuccessful(IPatcherRun patcher, string outputPath)
        {
            _wrapped.ReportRunSuccessful(patcher, outputPath);
        }

        public void ReportRunSuccessful(object? key, IPatcherRun patcher, string outputPath)
        {
            _wrapped.ReportRunSuccessful(patcher, outputPath);
        }

        public void ReportStartingRun(IPatcherRun patcher)
        {
            _wrapped.ReportStartingRun(patcher);
        }

        public void ReportStartingRun(object? key, IPatcherRun patcher)
        {
            _wrapped.ReportStartingRun(patcher);
        }
    }
}
