using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IExecutionParameters
    {
        string Parameters { get; }
    }

    [ExcludeFromCodeCoverage]
    public class ExecutionParameters : IExecutionParameters
    {
        public string Parameters => "--runtime win-x64 -c Release";
    }
}