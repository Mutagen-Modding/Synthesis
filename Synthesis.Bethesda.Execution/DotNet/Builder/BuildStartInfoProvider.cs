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
        public IExecutionParameters ExecutionParameters { get; }
        public IDotNetCommandStartConstructor StartConstructor { get; }

        public BuildStartInfoProvider(
            IExecutionParameters executionParameters,
            IDotNetCommandStartConstructor startConstructor)
        {
            ExecutionParameters = executionParameters;
            StartConstructor = startConstructor;
        }
        
        public ProcessStartInfo Construct(FilePath path)
        {
            return StartConstructor.Construct("build", path, ExecutionParameters.Parameters);
        }
    }
}