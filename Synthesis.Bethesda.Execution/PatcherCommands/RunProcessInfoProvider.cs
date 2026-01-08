using System.Diagnostics;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.PatcherCommands;

public interface IRunProcessStartInfoProvider
{
    ProcessStartInfo GetStart<T>(string path, bool directExe, T args);
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

    public ProcessStartInfo GetStart<T>(string path, bool directExe, T args)
    {
        var formattedArgs = Format.Format(args);
        if (directExe)
        {
            return new ProcessStartInfo(path, formattedArgs);
        }
        else
        {
            return ProjectRunProcessStartInfoProvider.GetStart(path, formattedArgs);
        }
    }
}