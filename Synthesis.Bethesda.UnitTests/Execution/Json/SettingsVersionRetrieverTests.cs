using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Json;

public class SettingsVersionRetrieverTests
{
    [Theory, SynthAutoData]
    public void NoVersionReturnsNull(
        MockFileSystem fileSystem,
        FilePath path,
        SettingsVersionRetriever sut)
    {
        fileSystem.File.WriteAllText(path, "{}");
        sut.GetVersion(path).Should().BeNull();
    }
        
    [Theory, SynthAutoData]
    public void ParsesVersion(
        MockFileSystem fileSystem,
        FilePath path,
        SettingsVersionRetriever sut)
    {
        fileSystem.File.WriteAllText(path, "{ \"Version\": \"3\" }");
        sut.GetVersion(path).Should().Be(3);
    }
}