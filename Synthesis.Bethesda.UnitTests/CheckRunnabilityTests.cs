using CommandLine;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class CheckRunnabilityTests
    {
        [Fact]
        public async Task Failure()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimSE);
            (await new SynthesisPipeline()
                .AddRunnabilityCheck(state =>
                {
                    throw new ArithmeticException();
                })
                .Run($"check-runnability -g SkyrimSE -d {dataFolder.Dir.Path}".Split(' ')))
                .Should().Be((int)Codes.NotRunnable);
        }

        [Fact]
        public async Task PassingCheck()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.SkyrimSE);
            (await new SynthesisPipeline()
                .AddRunnabilityCheck(state =>
                {
                    // Looks good?
                })
                .Run($"check-runnability -g SkyrimSE -d {dataFolder.Dir.Path}".Split(' ')))
                .Should().Be(0);
        }
    }
}
