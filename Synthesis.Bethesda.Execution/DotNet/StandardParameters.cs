namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IStandardParameters
    {
        string Parameters { get; }
    }

    public class StandardParameters : IStandardParameters
    {
        public string Parameters => "--runtime win-x64 -c Release";
    }
}