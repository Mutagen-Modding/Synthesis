using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Cli;

public class RunProfileProviderTests
{
    [Theory, SynthAutoData(ConfigureMembers: false)]
    public void ReturnsFirstProfileOfTargetName(
        string targetName,
        string someId,
        ISynthesisProfileSettings[] profileSettings,
        RunProfileProvider sut)
    {
        sut.PipelineProfilesProvider.Get().Returns(profileSettings);
        sut.ProfileNameProvider.Name.Returns(targetName);

        profileSettings[^2].Nickname = targetName;
        profileSettings[^2].ID = someId;
        profileSettings[^1].Nickname = targetName;
            
        sut.Get()
            .Should().BeSameAs(profileSettings[^2]);
    }

    [Theory, SynthAutoData(ConfigureMembers: false)]
    public void NullProfileIdThrows(
        string targetName,
        ISynthesisProfileSettings[] profileSettings,
        RunProfileProvider sut)
    {
        sut.PipelineProfilesProvider.Get().Returns(profileSettings);
        sut.ProfileNameProvider.Name.Returns(targetName);

        profileSettings[^1].Nickname = targetName;
        profileSettings[^1].ID = string.Empty;

        Assert.Throws<ArgumentException>(() => { sut.Get(); });
    }

    [Theory, SynthAutoData(ConfigureMembers: false)]
    public void NoMatchingProfileThrows(
        string targetName,
        ISynthesisProfileSettings[] profileSettings,
        RunProfileProvider sut)
    {
        sut.PipelineProfilesProvider.Get().Returns(profileSettings);
        sut.ProfileNameProvider.Name.Returns(targetName);

        Assert.Throws<ArgumentException>(() => { sut.Get(); });
    }

    [Theory, SynthAutoData(ConfigureMembers: false)]
    public void EmptyNameThrowsIfMultipleProfiles(
        ISynthesisProfileSettings[] profileSettings,
        RunProfileProvider sut)
    {
        sut.PipelineProfilesProvider.Get().Returns(profileSettings);
        sut.ProfileNameProvider.Name.Returns(string.Empty);

        Assert.Throws<ArgumentException>(() => { sut.Get(); });
    }

    [Theory, SynthAutoData(ConfigureMembers: false)]
    public void EmptyNameReturnsIfOnlyOneProfile(
        string someId,
        ISynthesisProfileSettings profileSetting,
        RunProfileProvider sut)
    {
        profileSetting.ID = someId;
        sut.PipelineProfilesProvider.Get().Returns(profileSetting.AsEnumerable().ToArray());
        sut.ProfileNameProvider.Name.Returns(string.Empty);

        sut.Get()
            .Should().BeSameAs(profileSetting);
    }
}