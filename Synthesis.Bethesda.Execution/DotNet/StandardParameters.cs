using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IStandardParameters
    {
        string Parameters { get; }
    }

    [ExcludeFromCodeCoverage]
    public class StandardParameters : IStandardParameters
    {
        public string Parameters => "--runtime win-x64 -c Release";
    }
}