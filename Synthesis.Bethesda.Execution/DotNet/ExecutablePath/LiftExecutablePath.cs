using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath;

public interface ILiftExecutablePath
{
    bool TryGet(IEnumerable<string> lines, [MaybeNullWhen(false)] out string output);
}

public class LiftExecutablePath : ILiftExecutablePath
{
    public const string Delimiter = " -> ";
        
    public bool TryGet(IEnumerable<string> lines, [MaybeNullWhen(false)] out string output)
    {
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!trimmed.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
            var index = trimmed.IndexOf(Delimiter, StringComparison.Ordinal);
            if (index == -1) continue;
            output = trimmed.Substring(index + Delimiter.Length).Trim();
            return true;
        }
        output = null;
        return false;
    }
}