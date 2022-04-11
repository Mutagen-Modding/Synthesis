using System.IO;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.TopLevel;

public class PatcherExtraDataPathProviderTests
{
    [Theory, SynthAutoData]
    public void CombinesExtraDataWithNameProvider(
        DirectoryPath extraData,
        string profileName,
        string name,
        PatcherExtraDataPathProvider sut)
    {
        sut.ExtraDataPathProvider.Path.Returns(extraData);
        sut.NameProvider.Name.Returns(name);
        sut.ProfileNameProvider.Name.Returns(profileName);
        sut.Path.Should().Be(
            new DirectoryPath(Path.Combine(extraData, profileName, name)));
    }
}