using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Shouldly;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Running.Runner;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class SplitModsAdjacencyValidatorTests
{
    private readonly SplitModsAdjacencyValidator _sut = new();

    private static IList<ILoadOrderListingGetter> CreateLoadOrder(params string[] modNames)
    {
        return modNames.Select(name => (ILoadOrderListingGetter)new LoadOrderListing(ModKey.FromFileName(name), true)).ToList();
    }

    [Fact]
    public void NoSplitMods_Passes()
    {
        var loadOrder = CreateLoadOrder("A.esp", "B.esp", "C.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void SingleMod_NoSplits_Passes()
    {
        var loadOrder = CreateLoadOrder("Patch.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void AdjacentSplitMods_Passes()
    {
        var loadOrder = CreateLoadOrder("A.esp", "Patch.esp", "Patch_2.esp", "Patch_3.esp", "B.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void AdjacentSplitMods_BaseFirst_Passes()
    {
        var loadOrder = CreateLoadOrder("Patch.esp", "Patch_2.esp", "Patch_3.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void NonAdjacentSplitMods_Throws()
    {
        var loadOrder = CreateLoadOrder("A.esp", "Patch.esp", "B.esp", "Patch_2.esp", "C.esp", "Patch_3.esp");

        var ex = Should.Throw<NonAdjacentSplitModsException>(() => _sut.Validate(loadOrder));
        ex.BaseModKey.FileName.String.ShouldBe("Patch.esp");
        ex.SplitModKeys.Count.ShouldBe(3);
    }

    [Fact]
    public void NonAdjacentSplitMods_BaseAndOneSplit_Throws()
    {
        var loadOrder = CreateLoadOrder("Patch.esp", "A.esp", "Patch_2.esp");

        var ex = Should.Throw<NonAdjacentSplitModsException>(() => _sut.Validate(loadOrder));
        ex.BaseModKey.FileName.String.ShouldBe("Patch.esp");
        ex.SplitModKeys.Count.ShouldBe(2);
    }

    [Fact]
    public void MultipleSplitSets_AllAdjacent_Passes()
    {
        var loadOrder = CreateLoadOrder(
            "A.esp",
            "Patch1.esp", "Patch1_2.esp",
            "B.esp",
            "Patch2.esp", "Patch2_2.esp", "Patch2_3.esp",
            "C.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void MultipleSplitSets_OneNonAdjacent_Throws()
    {
        var loadOrder = CreateLoadOrder(
            "A.esp",
            "Patch1.esp", "Patch1_2.esp",
            "B.esp",
            "Patch2.esp", "C.esp", "Patch2_2.esp"); // Patch2 split is non-adjacent

        var ex = Should.Throw<NonAdjacentSplitModsException>(() => _sut.Validate(loadOrder));
        ex.BaseModKey.FileName.String.ShouldBe("Patch2.esp");
    }

    [Fact]
    public void ModNamedWithSuffix_WithoutBase_Passes()
    {
        // SomeMod_2.esp without SomeMod.esp should not be considered a split set
        var loadOrder = CreateLoadOrder("A.esp", "SomeMod_2.esp", "B.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void OnlyBaseMod_WithoutSplits_Passes()
    {
        // Only base mod exists without any _N suffixed mods
        var loadOrder = CreateLoadOrder("A.esp", "Patch.esp", "B.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void SplitModsWithDifferentExtensions_Adjacent_Passes()
    {
        // Split mods can have different extensions (esp, esm, esl)
        var loadOrder = CreateLoadOrder("A.esp", "Patch.esm", "Patch_2.esp", "B.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void EmptyLoadOrder_Passes()
    {
        var loadOrder = CreateLoadOrder();

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void ModWithNumberInName_NotSplitPattern_Passes()
    {
        // Mods like "Mod2.esp" or "Mod_V2.esp" should not be treated as split mods
        var loadOrder = CreateLoadOrder("Mod2.esp", "A.esp", "Mod_V2.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void SplitNumber1_NotConsidered_Passes()
    {
        // _1 suffix should not be considered a split (splits start at _2)
        var loadOrder = CreateLoadOrder("Patch_1.esp", "A.esp", "Patch.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void CaseInsensitiveBaseName_Adjacent_Passes()
    {
        // Base name matching should be case-insensitive
        var loadOrder = CreateLoadOrder("patch.esp", "Patch_2.esp", "PATCH_3.esp");

        Should.NotThrow(() => _sut.Validate(loadOrder));
    }

    [Fact]
    public void CaseInsensitiveBaseName_NonAdjacent_Throws()
    {
        var loadOrder = CreateLoadOrder("patch.esp", "A.esp", "Patch_2.esp");

        var ex = Should.Throw<NonAdjacentSplitModsException>(() => _sut.Validate(loadOrder));
        ex.SplitModKeys.Count.ShouldBe(2);
    }
}
