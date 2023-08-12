using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath;

public interface ILiftExecutablePath
{
    bool TryGet(IEnumerable<string> lines, [MaybeNullWhen(false)] out string output);
}

public class LiftExecutablePath : ILiftExecutablePath
{
    public const string Delimiter = " -> ";

    private string? Get(ReadOnlySpan<char> line)
    {
        var trimmed = line.Trim();
        if (!trimmed.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return null;
        var index = trimmed.IndexOf(Delimiter, StringComparison.Ordinal);
        if (index == -1) return null;
        return trimmed.Slice(index + Delimiter.Length).Trim().ToString();
    }
        
    public bool TryGet(IEnumerable<string> lines, [MaybeNullWhen(false)] out string output)
    {
        output = lines.Select(x => Get(x)).NotNull().LastOrDefault();
        return output != null;
    }
}