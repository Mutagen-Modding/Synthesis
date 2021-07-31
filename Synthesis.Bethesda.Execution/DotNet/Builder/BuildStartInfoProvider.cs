using System.Diagnostics;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet.Builder
{
    public interface IBuildStartInfoProvider
    {
        ProcessStartInfo Construct(FilePath path);
    }

    public class BuildStartInfoProvider : IBuildStartInfoProvider
    {
        public IDotNetCommandStartConstructor StartConstructor { get; }

        public BuildStartInfoProvider(
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