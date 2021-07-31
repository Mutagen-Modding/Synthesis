using System.Diagnostics;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IProjectRunProcessStartInfoProvider
    {
        ProcessStartInfo GetStart(string path, string args, bool build = false);
    }

    public class ProjectRunProcessStartInfoProvider : IProjectRunProcessStartInfoProvider
    {
        public IDotNetCommandStartConstructor CmdStartConstructor { get; }

        public ProjectRunProcessStartInfoProvider(
            IDotNetCommandStartConstructor cmdStartConstructor)
        {
            CmdStartConstructor = cmdStartConstructor;
        }
        
        public ProcessStartInfo GetStart(string path, string args, bool build = false)
        {
            return CmdStartConstructor.Construct("run --project", path, 
                build ? string.Empty : "--no-build",
                args);
        }
    }
}