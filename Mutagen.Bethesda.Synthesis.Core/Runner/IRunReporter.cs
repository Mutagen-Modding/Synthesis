using Mutagen.Bethesda.Synthesis.Core.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.Core.Runner
{
    public interface IRunReporter
    {
        void ReportOverallProblem(Exception ex);
        void ReportPrepProblem(IPatcher patcher, Exception ex);
        void ReportRunProblem(IPatcher patcher, Exception ex);
        void ReportOutputMapping(IPatcher patcher, string str);
    }
}
