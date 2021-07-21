using Synthesis.Bethesda.Execution.Patchers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Noggog.Utility;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class CliPatcherTests
    {
        [Fact]
        public async Task MissingTarget()
        {
            var runSettings = new RunSynthesisPatcher();
            var factory = Substitute.For<IProcessFactory>();
            factory.Create(
                    Arg.Any<ProcessStartInfo>(),
                    Arg.Any<CancellationToken?>(),
                    Arg.Any<bool>(),
                    Arg.Any<bool>(),
                    Arg.Any<bool>())
                .Returns(x =>
                {
                    var process = Substitute.For<IProcessWrapper>();
                    process.Run().ThrowsForAnyArgs(_ => new Win32Exception());
                    return process;
                });
            var run = new CliPatcherRun(
                factory,
                "Missing", 
                "Missing.exe", 
                pathToExtra: null);
            await run.Prep(Mutagen.Bethesda.GameRelease.Oblivion, CancellationToken.None);
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await run.Run(runSettings, CancellationToken.None);
            });
        }
    }
}
