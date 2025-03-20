using Shouldly;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Synthesis.Bethesda.UnitTests.Common;

namespace Synthesis.Bethesda.UnitTests;

public class CheckRunnabilityTests
{
    [Fact]
    public async Task Failure()
    {
        var env = Utility.SetupEnvironment(GameRelease.SkyrimSE);
        (await new SynthesisPipeline()
                .AddRunnabilityCheck(state =>
                {
                    throw new ArithmeticException();
                })
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run($"check-runnability -g SkyrimSE -d {env.DataFolder}".Split(' '), fileSystem: env.FileSystem))
            .ShouldBe((int)Codes.NotRunnable);
    }
    
    [Fact]
    public async Task NoApplicablePatchTargetIsFailure()
    {
        var env = Utility.SetupEnvironment(GameRelease.SkyrimSE);
        (await new SynthesisPipeline()
                .AddRunnabilityCheck(state =>
                {
                    throw new ArithmeticException();
                })
                .AddPatch<IFallout4Mod, IFallout4ModGetter>(RunPatch)
                .Run($"check-runnability -g SkyrimSE -d {env.DataFolder}".Split(' '), fileSystem: env.FileSystem))
            .ShouldBe((int)Codes.NotRunnable);
    }

    [Fact]
    public async Task PassingCheck()
    {
        var env = Utility.SetupEnvironment(GameRelease.SkyrimSE);
        (await new SynthesisPipeline()
                .AddRunnabilityCheck(state =>
                {
                    // Looks good?
                })
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run($"check-runnability -g SkyrimSE -d {env.DataFolder}".Split(' '), fileSystem: env.FileSystem))
            .ShouldBe(0);
    }

    private static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        
    }

    private static void RunPatch(IPatcherState<IFallout4Mod, IFallout4ModGetter> state)
    {
        
    }
}