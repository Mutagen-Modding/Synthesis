using System.Diagnostics;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IBuildStartProvider
    {
        ProcessStartInfo Construct(FilePath path);
    }

    public class BuildStartProvider : IBuildStartProvider
    {
        public IDotNetCommandStartConstructor StartConstructor { get; }

        public BuildStartProvider(
            IDotNetCommandStartConstructor startConstructor)
        {
            StartConstructor = startConstructor;
        }
        
        public ProcessStartInfo Construct(FilePath path)
        {
            return StartConstructor.Construct("build", path);
        }
    }
}