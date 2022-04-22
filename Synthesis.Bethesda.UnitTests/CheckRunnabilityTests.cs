using FluentAssertions;
using Mutagen.Bethesda;
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
                .Run($"check-runnability -g SkyrimSE -d {env.DataFolder}".Split(' '), fileSystem: env.FileSystem))
            .Should().Be(0);
    }
}