using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Patchers.Common
{
    public interface IPatcherNameProvider
    {
        public string Name { get; }
    }
}