using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath;

public interface IRetrieveExecutablePath
{
    bool TryGet(FilePath projPath, IEnumerable<string> lines, [MaybeNullWhen(false)] out string output);
}

public class RetrieveExecutablePath : IRetrieveExecutablePath
{
    public ILiftExecutablePath Lift { get; }
    public IProcessExecutablePath Process { get; }

    public RetrieveExecutablePath(
        ILiftExecutablePath lift,
        IProcessExecutablePath process)
    {
        Lift = lift;
        Process = process;
    }

    public bool TryGet(FilePath projPath, IEnumerable<string> lines, [MaybeNullWhen(false)] out string output)
    {
        if (!Lift.TryGet(lines, out var execPath))
        {
            output = default;
            return false;
        }

        output = Process.Process(projPath, execPath);
        return true;
    }
}