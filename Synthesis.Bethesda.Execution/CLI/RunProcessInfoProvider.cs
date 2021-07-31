using System.Diagnostics;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IRunProcessStartInfoProvider
    {
        ProcessStartInfo GetStart<T>(string path, bool directExe, T args, bool build = false);
    }

    public class RunProcessStartInfoProvider : IRunProcessStartInfoProvider
    {
        public IFormatCommandLine Format { get; }
        public IProjectRunProcessStartInfoProvider ProjectRunProcessStartInfoProvider { get; }

        public RunProcessStartInfoProvider(
            IFormatCommandLine format,
            IProjectRunProcessStartInfoProvider projectRunProcessStartInfoProvider)
        {
            Format = format;
            ProjectRunProcessStartInfoProvider = projectRunProcessStartInfoProvider;
        }
        
        public ProcessStartInfo GetStart<T>(string path, bool directExe, T args, bool build = false)
        {
            var formattedArgs = Format.Format(args);
            if (directExe)
            {
                return new ProcessStartInfo(path, formattedArgs);
            }
            else
            {
                return ProjectRunProcessStartInfoProvider.GetStart(path, formattedArgs, build);
            }
        }
    }
}