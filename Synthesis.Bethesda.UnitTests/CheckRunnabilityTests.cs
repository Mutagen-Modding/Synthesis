using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Xunit;

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
            .Should().Be((int)Codes.NotRunnable);
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
            .Should().Be((int)Codes.NotRunnable);
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
            .Should().Be(0);
    }

    public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        
    }

    public static void RunPatch(IPatcherState<IFallout4Mod, IFallout4ModGetter> state)
    {
        
    }
}