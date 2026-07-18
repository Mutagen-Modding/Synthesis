using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.IntegrationTests.Components;

/// <summary>
/// Test injection for IBlockBuildingWithinMo2SettingsProvider that allows controlling the result
/// </summary>
public class BlockBuildingWithinMo2Injection : IBlockBuildingWithinMo2SettingsProvider
{
    public bool BlockBuildingWithinMo2 { get; set; }

    public BlockBuildingWithinMo2Injection(bool blockBuildingWithinMo2 = false)
    {
        BlockBuildingWithinMo2 = blockBuildingWithinMo2;
    }
}
