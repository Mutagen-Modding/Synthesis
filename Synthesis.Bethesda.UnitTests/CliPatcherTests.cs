using Synthesis.Bethesda.Execution.Patchers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
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
            var pathToExt = Substitute.For<IPathToExecutableInputProvider>();
            pathToExt.Path.Returns(new FilePath("Missing.exe"));
            var extraDataPath = Substitute.For<IPatcherExtraDataPathProvider>();
            extraDataPath.Path.Returns(new DirectoryPath());
            var run = new CliPatcherRun(
                factory,
                new PatcherNameInjection() { Name = "Missing" }, 
                pathToExt, 
                extraDataPath);
            await run.Prep(Mutagen.Bethesda.GameRelease.Oblivion, CancellationToken.None);
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await run.Run(runSettings, CancellationToken.None);
            });
        }
    }
}
