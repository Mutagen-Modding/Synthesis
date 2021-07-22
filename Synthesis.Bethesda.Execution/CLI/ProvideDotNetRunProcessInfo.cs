using System.Diagnostics;
using CommandLine;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IProvideDotNetRunProcessInfo
    {
        ProcessStartInfo GetStart(string path, bool directExe, object args, bool build = false);
    }

    public class ProvideDotNetRunProcessInfo : IProvideDotNetRunProcessInfo
    {
        private readonly IDotNetCommandStartConstructor _cmdStartConstructor;

        public ProvideDotNetRunProcessInfo(
            ICommandStringConstructor cmdStringConstructor,
            IDotNetCommandStartConstructor cmdStartConstructor)
        {
            _cmdStartConstructor = cmdStartConstructor;
        }
        
        public ProcessStartInfo GetStart(string path, bool directExe, object args, bool build = false)
        {
            if (directExe)
            {
                return new ProcessStartInfo(path, Parser.Default.FormatCommandLine(args));
            }
            else
            {
                return _cmdStartConstructor.Construct("run --project", path, 
                    build ? string.Empty : "--no-build",
                    Parser.Default.FormatCommandLine(args));
            }
        }
    }
}