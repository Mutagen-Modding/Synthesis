using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    public interface ILogger
    {
        void ReportOutput(string str);
        void ReportError(string str);
    }
}
