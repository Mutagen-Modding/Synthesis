using System.IO;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class EnsureSourcePathExistsTests
{
    [Theory, SynthAutoData]
    public void SourcePathNullDoesNotThrow(
        EnsureSourcePathExists sut)
    {
        sut.Ensure(null);
    }

    [Theory, SynthAutoData]
    public void HasValueButDoesNotExistThrows(
        ModPath missingPath,
        EnsureSourcePathExists sut)
    {
        Assert.Throws<FileNotFoundException>(() =>
        {
            sut.Ensure(missingPath);
        });
    }

    [Theory, SynthAutoData]
    public void HasValueAndExistDoesNotThrow(
        ModPath existingPath,
        EnsureSourcePathExists sut)
    {
        sut.Ensure(existingPath);
    }
}