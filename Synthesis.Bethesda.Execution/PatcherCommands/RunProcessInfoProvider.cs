using System.Diagnostics;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.PatcherCommands;

public interface IRunProcessStartInfoProvider
{
    ProcessStartInfo GetStart<T>(string executablePath, T args);
}

public class RunProcessStartInfoProvider : IRunProcessStartInfoProvider
{
    public IFormatCommandLine Format { get; }

    public RunProcessStartInfoProvider(
        IFormatCommandLine format)
    {
        Format = format;
    }

    public ProcessStartInfo GetStart<T>(string executablePath, T args)
    {
        var formattedArgs = Format.Format(args);
        return new ProcessStartInfo(executablePath, formattedArgs);
    }
}