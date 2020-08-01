using Mutagen.Bethesda.Synthesis.Core.Patchers;
using Mutagen.Bethesda.Synthesis.Core.Runner;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mutagen.Bethesda.Synthesis.UnitTests
{
    public class RunnerTests
    {
        [Fact]
        public async Task EmptyRun()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output, 
                patchers: ListExt.Empty<IPatcher>());
            Assert.False(File.Exists(output));
        }

        [Fact]
        public async Task ListedNonExistantSourcePath()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var patcher = new DummyPatcher();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                sourcePath: output,
                reporter: reporter,
                patchers: patcher.AsEnumerable().ToList());
            Assert.False(patcher.WasPrepped);
            Assert.IsType<FileNotFoundException>(reporter.Overall);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task BasicPatcherFunctionsCalled()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                patchers: patcher.AsEnumerable().ToList());
            Assert.True(patcher.WasRun);
            Assert.True(patcher.WasPrepped);
        }

        [Fact]
        public async Task ChecksIfPatchersOutput()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher()
            {
                DoWork = false,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                reporter: reporter,
                patchers: patcher.AsEnumerable().ToList());
            Assert.IsType<ArgumentException>(reporter.RunProblem?.Exception);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task FinalOutputFileCreated()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                patchers: patcher.AsEnumerable().ToList());
            Assert.True(File.Exists(output));
        }

        [Fact]
        public async Task PatcherThrowInPrep()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher()
            {
                ThrowInPrep = true,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                patchers: patcher.AsEnumerable().ToList(),
                reporter: reporter);
            Assert.False(File.Exists(output));
            Assert.True(patcher.WasPrepped);
            Assert.False(patcher.WasRun);
            Assert.Equal(1, reporter.PrepProblems.Count);
            Assert.IsType<NotImplementedException>(reporter.PrepProblems[0].Exception);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task PatcherThrowInRun()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher()
            {
                ThrowInRun = true,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                patchers: patcher.AsEnumerable().ToList(),
                reporter: reporter);
            Assert.False(File.Exists(output));
            Assert.True(patcher.WasPrepped);
            Assert.True(patcher.WasRun);
            Assert.Equal(0, reporter.PrepProblems.Count);
            Assert.IsType<NotImplementedException>(reporter.RunProblem?.Exception);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task PatcherOutputReported()
        {
            using var tmpFolder = Utility.GetTempFolder();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher();
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                patchers: patcher.AsEnumerable().ToList(),
                reporter: reporter);
            Assert.True(File.Exists(output));
            Assert.True(patcher.WasPrepped);
            Assert.True(patcher.WasRun);
            Assert.True(reporter.Success);
            Assert.True(reporter.Output.Count > 0);
            Assert.NotEqual(output, reporter.Output[0].Output);
            Assert.True(File.Exists(reporter.Output[0].Output));
        }

        public class DummyPatcher : IPatcher
        {
            public string Name => "Dummy";

            public bool WasDisposed;
            public bool WasPrepped;
            public bool WasRun;
            public bool ThrowInPrep;
            public bool ThrowInRun;
            public bool DoWork = true;

            public void Dispose()
            {
                WasDisposed = true;
            }

            public async Task Prep(CancellationToken? cancel = null)
            {
                WasPrepped = true;
                if (ThrowInPrep)
                {
                    throw new NotImplementedException();
                }
            }

            public async Task Run(ModPath? sourcePath, ModPath outputPath)
            {
                if (DoWork)
                {
                    File.WriteAllText(outputPath.Path, "Hello");
                }
                WasRun = true;
                if (ThrowInRun)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
