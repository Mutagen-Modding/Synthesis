using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet.NugetListing;

public interface INugetListingParser
{
    bool TryParse(
        string line, 
        [MaybeNullWhen(false)] out string package,
        [MaybeNullWhen(false)] out string requested,
        [MaybeNullWhen(false)] out string resolved,
        [MaybeNullWhen(false)] out string latest);
}

public class NugetListingParser : INugetListingParser
{
    public bool TryParse(
        string line, 
        [MaybeNullWhen(false)] out string package,
        [MaybeNullWhen(false)] out string requested,
        [MaybeNullWhen(false)] out string resolved,
        [MaybeNullWhen(false)] out string latest)
    {
        var startIndex = line.IndexOf("> ");
        if (startIndex == -1)
        {
            package = default;
            requested = default;
            resolved = default;
            latest = default;
            return false;
        }
        var split = line
            .Substring(startIndex + 2)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .WithIndex()
            .Where(x => x.Index == 0 || x.Item != "(D)")
            .Select(x => x.Item)
            .ToArray();
        package = split[0];
        requested = split[1];
        resolved = split[2];
        latest = split[3];
        return true;
    }
}