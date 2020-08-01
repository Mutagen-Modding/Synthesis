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

        private readonly List<(IPatcher Patcher, Exception Exception)> _prepProblems = new List<(IPatcher Patcher, Exception Exception)>();
        public IReadOnlyList<(IPatcher Patcher, Exception Exception)> PrepProblems => _prepProblems;

        public (IPatcher Patcher, Exception Exception)? RunProblem { get; private set; }

        private readonly List<(IPatcher Patcher, string Output)> _outputMap = new List<(IPatcher Patcher, string Output)>();
        public IReadOnlyList<(IPatcher Patcher, string Output)> Output => _outputMap;

        public void ReportOverallProblem(Exception ex)
        {
            if (Overall != null)
            {
                throw new ArgumentException("Reported two overall exceptions.");
            }
            Overall = ex;
        }

        public void ReportPrepProblem(IPatcher patcher, Exception ex)
        {
            _prepProblems.Add((patcher, ex));
        }

        public void ReportRunProblem(IPatcher patcher, Exception ex)
        {
            if (RunProblem != null)
            {
                throw new ArgumentException("Reported two patcher run exceptions.");
            }
            RunProblem = (patcher, ex);
        }

        public void ReportOutputMapping(IPatcher patcher, string str)
        {
            _outputMap.Add((patcher, str));
        }
    }
}
