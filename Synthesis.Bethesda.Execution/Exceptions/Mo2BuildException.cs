using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Exceptions;

/// <summary>
/// Exception thrown when a build fails with an access denied error while running inside MO2's VFS.
/// Distinguishes MO2-caused build failures from unrelated access denied errors that can surface
/// elsewhere, so only genuine build failures are shown as an MO2 build problem.
/// </summary>
[ExcludeFromCodeCoverage]
public class Mo2BuildException : Exception
{
    public string FilePath { get; }

    public Mo2BuildException(string filePath)
        : base("Build failed due to an access denied error while running inside MO2's virtual file system (VFS).")
    {
        FilePath = filePath;
    }
}
