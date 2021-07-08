namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IBuildStringProvider
    {
        string Get(string args);
    }

    public class BuildStringProvider : IBuildStringProvider
    {
        public string Get(string args)
        {
            return $"build --runtime win-x64 {args} -c Release";
        }
    }
}