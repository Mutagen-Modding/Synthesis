using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class CliPatcherTests
    {
        [Fact]
        public async Task MissingTarget()
        {
            var runSettings = new RunSynthesisPatcher();
            var run = new CliPatcherRun("Missing", "Missing.exe", pathToExtra: null);
            await run.Prep(Mutagen.Bethesda.GameRelease.Oblivion, CancellationToken.None);
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await run.Run(runSettings, CancellationToken.None);
            });
        }
    }
}
