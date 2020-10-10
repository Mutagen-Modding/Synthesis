using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    public interface ILogger
    {
        void Write(string str);
        void WriteError(string str);
    }
}
