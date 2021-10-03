using System;
using Loqui;

namespace Synthesis.Bethesda.UnitTests
{
    public class LoquiUse : IDisposable
    {
        public LoquiRegister? Register;

        public LoquiUse()
        {
            Register = LoquiRegistration.StaticRegister;
        }

        public void Dispose()
        {
            Register= null;
        }
    }
}