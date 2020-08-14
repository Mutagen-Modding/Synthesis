using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var run = new CliPatcherRun("Missing", "Missing.exe");
            await run.Prep(Mutagen.Bethesda.GameRelease.Oblivion);
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await run.Run(runSettings);
            });
        }
    }
}
