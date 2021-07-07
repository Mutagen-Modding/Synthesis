namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IProvideBuildString
    {
        string Get(string args);
    }

    public class ProvideBuildString : IProvideBuildString
    {
        public string Get(string args)
        {
            return $"build --runtime win-x64 {args} -c Release";
        }
    }
}