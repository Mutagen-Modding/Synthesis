using Noggog;
using Synthesis.Bethesda.Execution.DotNet.Builder;

namespace Synthesis.Bethesda.IntegrationTests.TestUtilities;

/// <summary>
/// Test stub for IBuild that throws when called, used to verify build meta caching is working.
/// NOTE: This is in TestUtilities namespace (not Components) to avoid auto-registration.
/// It should only be registered explicitly in specific tests.
/// </summary>
public class ThrowingBuild : IBuild
{
    public Task<ErrorResponse> Compile(FilePath targetPath, CancellationToken cancel)
    {
        throw new InvalidOperationException(
            $"ThrowingBuild.Compile was called for {targetPath}. " +
            "This should not happen if build meta caching is working correctly.");
    }
}
