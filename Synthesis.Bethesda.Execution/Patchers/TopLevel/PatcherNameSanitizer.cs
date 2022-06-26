using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.TopLevel;

public interface IPatcherNameSanitizer
{
    string Sanitize(string name);
}

[ExcludeFromCodeCoverage]
public class PatcherNameSanitizer : IPatcherNameSanitizer
{
    public string Sanitize(string name)
    {
        return StringExt.RemoveDisallowedFilepathChars(name);
    }
}