using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath
{
    public interface ILiftExecutablePath
    {
        bool TryGet(IEnumerable<string> lines, [MaybeNullWhen(false)] out string output);
    }

    public class LiftExecutablePath : ILiftExecutablePath
    {
        public bool TryGet(IEnumerable<string> lines, [MaybeNullWhen(false)] out string output)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.EndsWith(".dll")) continue;
                const string delimiter = " -> ";
                var index = trimmed.IndexOf(delimiter, StringComparison.Ordinal);
                if (index == -1) continue;
                output = trimmed.Substring(index + delimiter.Length).Trim();
                return true;
            }
            output = null;
            return false;
        }
    }
}