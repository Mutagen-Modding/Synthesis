using System;
using Noggog;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget
{
    public interface INugetErrorSolution
    {
        string ErrorText { get; }
        void RunFix(FilePath path);
    }
}