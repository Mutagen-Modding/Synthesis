using System.Diagnostics;
using CommandLine;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IProvideDotNetProcessInfo
    {
        ProcessStartInfo GetStart(string path, bool directExe, object args, bool build = false);
    }

    public class ProvideDotNetProcessInfo : IProvideDotNetProcessInfo
    {
        public ProcessStartInfo GetStart(string path, bool directExe, object args, bool build = false)
        {
            if (directExe)
            {
                return new ProcessStartInfo(path, Parser.Default.FormatCommandLine(args));
            }
            else
            {
                return new ProcessStartInfo("dotnet", $"run --project \"{path}\" -c Release --runtime win-x64{(build ? string.Empty : " --no-build")} {Parser.Default.FormatCommandLine(args)}");
            }
        }
    }
}