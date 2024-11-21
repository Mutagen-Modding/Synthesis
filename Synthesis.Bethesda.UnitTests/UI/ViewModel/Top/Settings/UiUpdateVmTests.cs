using AutoFixture.Xunit2;
using FluentAssertions;
using Mutagen.Bethesda.Synthesis.Versioning;
using NSubstitute;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.GUI.Services.Versioning;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.UI.ViewModel.Top.Settings;

public class UiUpdateVmTests
{
    [Theory]
    [SynthAutoData]
    public async Task SameVersionHasNoUpdate(
        [Frozen] IProvideCurrentVersions currentVersions,
        [Frozen] INewestLibraryVersionsVm libraryVersions,
        Lazy<UiUpdateVm> sutGetter)
    {
        currentVersions.SynthesisVersion.Returns("1.0.0");
        libraryVersions.Versions.Returns(new NugetVersionOptions(
            new NugetVersionPair("2.0.0", "1.0.0"),
            new NugetVersionPair("2.0.0-pr01", "1.0.0-pr01")));
        var sut = sutGetter.Value;
        sut.HasUpdate.Should().BeFalse();
    }
    
    [Theory]
    [SynthAutoData]
    public async Task TypicalUpdate(
        [Frozen] IProvideCurrentVersions currentVersions,
        [Frozen] INewestLibraryVersionsVm libraryVersions,
        Lazy<UiUpdateVm> sutGetter)
    {
        currentVersions.SynthesisVersion.Returns("1.0.0");
        libraryVersions.Versions.Returns(new NugetVersionOptions(
            new NugetVersionPair("2.0.0", "1.1.0"),
            new NugetVersionPair("2.0.0-pr01", "1.0.0-pr01")));
        var sut = sutGetter.Value;
        sut.HasUpdate.Should().BeTrue();
    }
    
    [Theory]
    [SynthAutoData]
    public async Task PreReleaseNotSeenAsUpdate(
        [Frozen] IProvideCurrentVersions currentVersions,
        [Frozen] INewestLibraryVersionsVm libraryVersions,
        Lazy<UiUpdateVm> sutGetter)
    {
        currentVersions.SynthesisVersion.Returns("1.0.0");
        libraryVersions.Versions.Returns(new NugetVersionOptions(
            new NugetVersionPair("2.0.0", "1.0.0"),
            new NugetVersionPair("2.0.0-pr01", "1.1.0-pr01")));
        var sut = sutGetter.Value;
        sut.HasUpdate.Should().BeFalse();
    }
    
    [Theory]
    [SynthAutoData]
    public async Task BothPrereleasesSeenAsUpdate(
        [Frozen] IProvideCurrentVersions currentVersions,
        [Frozen] INewestLibraryVersionsVm libraryVersions,
        Lazy<UiUpdateVm> sutGetter)
    {
        currentVersions.SynthesisVersion.Returns("1.0.0-pr01");
        libraryVersions.Versions.Returns(new NugetVersionOptions(
            new NugetVersionPair("2.0.0", "1.0.0"),
            new NugetVersionPair("2.0.0-pr01", "1.1.0-pr01")));
        var sut = sutGetter.Value;
        sut.HasUpdate.Should().BeTrue();
    }
}