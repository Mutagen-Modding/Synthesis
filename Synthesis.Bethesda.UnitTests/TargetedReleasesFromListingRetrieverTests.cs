using Mutagen.Bethesda;
using Shouldly;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Xunit;

namespace Synthesis.Bethesda.UnitTests;

public class TargetedReleasesFromListingRetrieverTests
{
    private static readonly TargetedReleasesFromListingRetriever Sut = new();

    [Fact]
    public void DeliberateSelectionIsHonored()
    {
        var listing = new PatcherListing()
        {
            IncludedLibraries = new[] { GameCategory.Skyrim },
            Customization = new PatcherCustomization()
            {
                TargetedReleases = new[] { GameRelease.SkyrimSE, GameRelease.SkyrimSEGog },
            },
        };

        Sut.Get(listing).ShouldBe(new[] { GameRelease.SkyrimSE, GameRelease.SkyrimSEGog });
    }

    [Fact]
    public void BlankFallsBackToIncludedLibraries()
    {
        var listing = new PatcherListing()
        {
            IncludedLibraries = new[] { GameCategory.Skyrim },
            Customization = new PatcherCustomization()
            {
                TargetedReleases = Array.Empty<GameRelease>(),
            },
        };

        Sut.Get(listing).ShouldBe(GameCategory.Skyrim.GetRelatedReleases(), ignoreOrder: true);
    }

    [Fact]
    public void NullCustomizationFallsBackToIncludedLibraries()
    {
        var listing = new PatcherListing()
        {
            IncludedLibraries = new[] { GameCategory.Fallout4 },
        };

        Sut.Get(listing).ShouldBe(GameCategory.Fallout4.GetRelatedReleases(), ignoreOrder: true);
    }

    [Fact]
    public void LegacySelectAllIsTreatedAsBlank()
    {
        // The old GUI default checked every box across every game family.  A Skyrim-only
        // patcher should collapse back to Skyrim releases rather than claim all games.
        var everyRelease = Enum.GetValues<GameCategory>()
            .SelectMany(x => x.GetRelatedReleases())
            .ToArray();
        var listing = new PatcherListing()
        {
            IncludedLibraries = new[] { GameCategory.Skyrim },
            Customization = new PatcherCustomization()
            {
                TargetedReleases = everyRelease,
            },
        };

        Sut.Get(listing).ShouldBe(GameCategory.Skyrim.GetRelatedReleases(), ignoreOrder: true);
    }

    [Fact]
    public void GenuinelyCrossGameStillResolvesToAllReleasesViaLibraries()
    {
        // A truly generic patcher references every library, so even though its all-releases
        // selection is treated as "no opinion", the library fallback yields the same set.
        var everyRelease = Enum.GetValues<GameCategory>()
            .SelectMany(x => x.GetRelatedReleases())
            .ToArray();
        var listing = new PatcherListing()
        {
            IncludedLibraries = Enum.GetValues<GameCategory>(),
            Customization = new PatcherCustomization()
            {
                TargetedReleases = everyRelease,
            },
        };

        Sut.Get(listing).ShouldBe(everyRelease, ignoreOrder: true);
    }

    [Fact]
    public void SingleFamilyMultiReleaseSelectionIsHonored()
    {
        // Targeting all Skyrim releases is a legitimate deliberate choice, not the legacy
        // select-all, because it does not span the Oblivion/Fallout4 families.
        var skyrim = GameCategory.Skyrim.GetRelatedReleases().ToArray();
        var listing = new PatcherListing()
        {
            IncludedLibraries = new[] { GameCategory.Skyrim },
            Customization = new PatcherCustomization()
            {
                TargetedReleases = skyrim,
            },
        };

        Sut.Get(listing).ShouldBe(skyrim);
    }
}
