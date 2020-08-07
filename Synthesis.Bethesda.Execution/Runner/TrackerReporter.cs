using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Runner
{
    public class TrackerReporter : IRunReporter
    {
        public bool Success => Overall == null
            && _prepProblems.Count == 0
            && RunProblem == null;

        public Exception? Overall { get; private set; }

        private readonly List<(IPatcherRun Patcher, Exception Exception)> _prepProblems = new List<(IPatcherRun Patcher, Exception Exception)>();
        public IReadOnlyList<(IPatcherRun Patcher, Exception Exception)> PrepProblems => _prepProblems;

        public (IPatcherRun Patcher, Exception Exception)? RunProblem { get; private set; }

        private readonly List<(IPatcherRun Patcher, string Output)> _outputMap = new List<(IPatcherRun Patcher, string Output)>();
        public IReadOnlyList<(IPatcherRun Patcher, string Output)> Output => _outputMap;

        public void ReportOverallProblem(Exception ex)
        {
            if (Overall != null)
            {
                throw new ArgumentException("Reported two overall exceptions.");
            }
            Overall = ex;
        }

        public void ReportPrepProblem(IPatcherRun patcher, Exception ex)
        {
            _prepProblems.Add((patcher, ex));
        }

        public void ReportRunProblem(IPatcherRun patcher, Exception ex)
        {
            if (RunProblem != null)
            {
                throw new ArgumentException("Reported two patcher run exceptions.");
            }
            RunProblem = (patcher, ex);
        }

        public void ReportOutputMapping(IPatcherRun patcher, string str)
        {
            _outputMap.Add((patcher, str));
        }
    }
}
