using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.UnitTests.AutoData;

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
        sut.GetVersion(path).ShouldBeNull();
    }
        
    [Theory, SynthAutoData]
    public void ParsesVersion(
        MockFileSystem fileSystem,
        FilePath path,
        SettingsVersionRetriever sut)
    {
        fileSystem.File.WriteAllText(path, "{ \"Version\": \"3\" }");
        sut.GetVersion(path).ShouldBe(3);
    }
}