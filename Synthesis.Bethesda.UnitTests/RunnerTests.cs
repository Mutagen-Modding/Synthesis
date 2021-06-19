using Synthesis.Bethesda.Execution.Patchers;
using Noggog;
using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Mutagen.Bethesda;
using System.Reactive.Linq;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Reporters;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins;
using Path = System.IO.Path;
using FileNotFoundException = System.IO.FileNotFoundException;

namespace Synthesis.Bethesda.UnitTests
{
    public class RunnerTests
    {
        [Fact]
        public async Task EmptyRun()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                loadOrder: env.GetTypicalLoadOrder(),
                release: GameRelease.Oblivion,
                patchers: ListExt.Empty<IPatcherRun>(),
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.False(env.FileSystem.File.Exists(output));
        }

        [Fact]
        public async Task ListedNonExistantSourcePath()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var patcher = new DummyPatcher(env.FileSystem);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.Oblivion,
                loadOrder: env.GetTypicalLoadOrder(),
                sourcePath: output,
                reporter: reporter,
                patchers: patcher.AsEnumerable().ToList(),
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.False(patcher.WasPrepped);
            Assert.IsType<FileNotFoundException>(reporter.Overall);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task BasicPatcherFunctionsCalled()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            var patcher = new DummyPatcher(env.FileSystem);
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                loadOrder: env.GetTypicalLoadOrder(),
                release: GameRelease.Oblivion,
                patchers: patcher.AsEnumerable().ToList(),
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.True(patcher.WasRun);
            Assert.True(patcher.WasPrepped);
        }

        [Fact]
        public async Task ChecksIfPatchersOutput()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            var patcher = new DummyPatcher(env.FileSystem)
            {
                DoWork = false,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.Oblivion,
                loadOrder: env.GetTypicalLoadOrder(),
                reporter: reporter,
                patchers: patcher.AsEnumerable().ToList(),
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.IsType<ArgumentException>(reporter.RunProblem?.Exception);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task FinalOutputFileCreated()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            var patcher = new DummyPatcher(env.FileSystem);
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.Oblivion,
                loadOrder: env.GetTypicalLoadOrder(),
                patchers: patcher.AsEnumerable().ToList(),
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.True(env.FileSystem.File.Exists(output));
        }

        [Fact]
        public async Task PatcherThrowInPrep()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            var patcher = new DummyPatcher(env.FileSystem)
            {
                ThrowInPrep = true,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.Oblivion,
                loadOrder: env.GetTypicalLoadOrder(),
                patchers: patcher.AsEnumerable().ToList(),
                reporter: reporter,
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.False(env.FileSystem.File.Exists(output));
            Assert.True(patcher.WasPrepped);
            Assert.False(patcher.WasRun);
            Assert.Equal(1, reporter.PrepProblems.Count);
            Assert.IsType<NotImplementedException>(reporter.PrepProblems[0].Exception);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task PatcherThrowInRun()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            var patcher = new DummyPatcher(env.FileSystem)
            {
                ThrowInRun = true,
            };
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.Oblivion,
                loadOrder: env.GetTypicalLoadOrder(),
                patchers: patcher.AsEnumerable().ToList(),
                reporter: reporter,
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.False(env.FileSystem.File.Exists(output));
            Assert.True(patcher.WasPrepped);
            Assert.True(patcher.WasRun);
            Assert.Equal(0, reporter.PrepProblems.Count);
            Assert.IsType<NotImplementedException>(reporter.RunProblem?.Exception);
            Assert.False(reporter.Success);
        }

        [Fact]
        public async Task PatcherOutputReported()
        {
            var env = Utility.SetupEnvironment(GameRelease.Oblivion);
            var output = Utility.TypicalOutputFile(env.BaseFolder);
            var patcher = new DummyPatcher(env.FileSystem);
            var reporter = new TrackerReporter();
            await Runner.Run(
                workingDirectory: env.BaseFolder,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.Oblivion,
                loadOrder: env.GetTypicalLoadOrder(),
                patchers: patcher.AsEnumerable().ToList(),
                reporter: reporter,
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.True(env.FileSystem.File.Exists(output));
            Assert.True(patcher.WasPrepped);
            Assert.True(patcher.WasRun);
            Assert.True(reporter.Success);
            Assert.True(reporter.PatcherComplete.Count > 0);
            Assert.NotEqual(output.Path, reporter.PatcherComplete[0].OutputPath);
            Assert.True(env.FileSystem.File.Exists(reporter.PatcherComplete[0].OutputPath));
        }

        [Fact]
        public async Task TrimsPostSynthesisFromLoadOrder()
        {
            var env = Utility.SetupEnvironment(GameRelease.SkyrimLE);
            var workingDir = Path.Combine(env.BaseFolder, "WorkingDir");
            var output = Utility.TypicalOutputFile(workingDir);
            var patcher = new DummyPatcher(env.FileSystem);
            await Runner.Run(
                workingDirectory: workingDir,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.SkyrimLE,
                loadOrder: env.GetTypicalLoadOrder()
                    .And(new ModListing(Constants.SynthesisModKey, true))
                    .And(new ModListing(Utility.RandomModKey, true)),
                patchers: patcher.AsEnumerable().ToList(),
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.Equal(
                new string[]
                {
                    Utility.TestModKey.FileName,
                    Utility.OverrideModKey.FileName,
                }, env.FileSystem.File.ReadAllLines(Path.Combine(workingDir, Utility.PathToLoadOrderFile)));
        }

        [Fact]
        public async Task TrimsAtypicalOutputFromLoadOrder()
        {
            var env = Utility.SetupEnvironment(GameRelease.SkyrimLE);
            ModKey atypicalKey = ModKey.FromNameAndExtension("Atypical.esp");
            var workingDir = Path.Combine(env.BaseFolder, "WorkingDir");
            var output = Path.Combine(workingDir, atypicalKey.FileName);
            var patcher = new DummyPatcher(env.FileSystem);
            await Runner.Run(
                workingDirectory: workingDir,
                outputPath: output,
                dataFolder: env.DataFolder,
                release: GameRelease.SkyrimLE,
                loadOrder: env.GetTypicalLoadOrder()
                    .And(new ModListing(Constants.SynthesisModKey, true))
                    .And(new ModListing(atypicalKey, true))
                    .And(new ModListing(Utility.RandomModKey, true)),
                patchers: patcher.AsEnumerable().ToList(),
                cancel: CancellationToken.None,
                fileSystem: env.FileSystem);
            Assert.Equal(
                new string[]
                {
                    Utility.TestModKey.FileName,
                    Utility.OverrideModKey.FileName,
                    Constants.SynthesisModKey.FileName,
                }, env.FileSystem.File.ReadAllLines(Path.Combine(workingDir, Utility.PathToLoadOrderFile)));
        }

        public class DummyPatcher : IPatcherRun
        {
            private readonly IFileSystem _FileSystem;
            public string Name => "Dummy";

            public bool WasDisposed;
            public bool WasPrepped;
            public bool WasRun;
            public bool ThrowInPrep;
            public bool ThrowInRun;
            public bool DoWork = true;

            public IObservable<string> Output => Observable.Empty<string>();

            public IObservable<string> Error => Observable.Empty<string>();

            public DummyPatcher(IFileSystem fileSystem)
            {
                _FileSystem = fileSystem;
            }

            public void Dispose()
            {
                WasDisposed = true;
            }

            public async Task Prep(GameRelease release, CancellationToken cancel)
            {
                WasPrepped = true;
                if (ThrowInPrep)
                {
                    throw new NotImplementedException();
                }
            }

            public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
            {
                if (DoWork)
                {
                    _FileSystem.File.WriteAllText(settings.OutputPath, "Hello");
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
