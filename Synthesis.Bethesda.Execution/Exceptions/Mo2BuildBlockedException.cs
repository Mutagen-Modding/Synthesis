using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Exceptions;

/// <summary>
/// Exception thrown when a build is blocked because the application is running inside MO2's VFS
/// with the BlockBuildingWithinMo2 setting enabled.
/// </summary>
[ExcludeFromCodeCoverage]
public class Mo2BuildBlockedException : Exception
{
    public Mo2BuildBlockedException()
        : base("Build blocked: MO2's virtual file system (VFS) is incompatible with .NET SDK builds.")
    {
    }
}
