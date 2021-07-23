using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath
{
    public interface IRetrieveExecutablePath
    {
        bool TryGet(FilePath projPath, IEnumerable<string> lines, [MaybeNullWhen(false)] out string output);
    }

    public class RetrieveExecutablePath : IRetrieveExecutablePath
    {
        private readonly ILiftExecutablePath _lift;
        private readonly IProcessExecutablePath _process;

        public RetrieveExecutablePath(
            ILiftExecutablePath lift,
            IProcessExecutablePath process)
        {
            _lift = lift;
            _process = process;
        }

        public bool TryGet(FilePath projPath, IEnumerable<string> lines, [MaybeNullWhen(false)] out string output)
        {
            if (!_lift.TryGet(lines, out var execPath))
            {
                output = default;
                return false;
            }

            output = _process.Process(projPath, execPath);
            return true;
        }
    }
}