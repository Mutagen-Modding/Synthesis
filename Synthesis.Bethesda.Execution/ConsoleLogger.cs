using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    public class Logger : ILogger
    {
        public void Write(string str) => System.Console.WriteLine(str);
        public void WriteError(string str) => System.Console.Error.Write(str);
    }
}
