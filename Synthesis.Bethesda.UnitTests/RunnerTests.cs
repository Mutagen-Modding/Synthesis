using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Runner;
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
using Mutagen.Bethesda;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.UnitTests
{
    public class RunnerTests
    {
        [Fact]
        public async Task EmptyRun()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(tmpFolder);
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
                release: GameRelease.Oblivion,
                patchers: ListExt.Empty<IPatcherRun>());
            Assert.False(File.Exists(output));
        }

        [Fact]
        public async Task ListedNonExistantSourcePath()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var patcher = new DummyPatcher();
            var output = Utility.TypicalOutputFile(tmpFolder);
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                release: GameRelease.Oblivion,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
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
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
                release: GameRelease.Oblivion,
                patchers: patcher.AsEnumerable().ToList());
            Assert.True(patcher.WasRun);
            Assert.True(patcher.WasPrepped);
        }

        [Fact]
        public async Task ChecksIfPatchersOutput()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher()
            {
                DoWork = false,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                release: GameRelease.Oblivion,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
                reporter: reporter,
                patchers: patcher.AsEnumerable().ToList());
            Assert.IsType<ArgumentException>(reporter.RunProblem?.Exception);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task FinalOutputFileCreated()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                release: GameRelease.Oblivion,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
                patchers: patcher.AsEnumerable().ToList());
            Assert.True(File.Exists(output));
        }

        [Fact]
        public async Task PatcherThrowInPrep()
        {
            using var tmpFolder = Utility.GetTempFolder();
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher()
            {
                ThrowInPrep = true,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                release: GameRelease.Oblivion,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
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
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher()
            {
                ThrowInRun = true,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                release: GameRelease.Oblivion,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
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
            using var dataFolder = Utility.SetupDataFolder(tmpFolder, GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(tmpFolder);
            var patcher = new DummyPatcher();
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: tmpFolder.Dir.Path,
                outputPath: output,
                dataFolder: dataFolder.Dir.Path,
                release: GameRelease.Oblivion,
                loadOrder: Utility.TypicalLoadOrder(GameRelease.Oblivion, dataFolder.Dir),
                patchers: patcher.AsEnumerable().ToList(),
                reporter: reporter);
            Assert.True(File.Exists(output));
            Assert.True(patcher.WasPrepped);
            Assert.True(patcher.WasRun);
            Assert.True(reporter.Success);
            Assert.True(reporter.PatcherComplete.Count > 0);
            Assert.NotEqual(output, reporter.PatcherComplete[0].OutputPath);
            Assert.True(File.Exists(reporter.PatcherComplete[0].OutputPath));
        }

        public class DummyPatcher : IPatcherRun
        {
            public string Name => "Dummy";

            public bool WasDisposed;
            public bool WasPrepped;
            public bool WasRun;
            public bool ThrowInPrep;
            public bool ThrowInRun;
            public bool DoWork = true;

            public IObservable<string> Output => Observable.Empty<string>();

            public IObservable<string> Error => Observable.Empty<string>();

            public void Dispose()
            {
                WasDisposed = true;
            }

            public async Task Prep(GameRelease release, CancellationToken? cancel = null)
            {
                WasPrepped = true;
                if (ThrowInPrep)
                {
                    throw new NotImplementedException();
                }
            }

            public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
            {
                if (DoWork)
                {
                    File.WriteAllText(settings.OutputPath, "Hello");
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
