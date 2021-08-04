using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class RunArgsConstructorTests
    {
        [Theory, SynthAutoData]
        public void PassesPatcherNameToSanitizer(
            IPatcherRun patcher,
            int key,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath,
            RunArgsConstructor sut)
        {
            sut.GetArgs(patcher, key, outputKey, sourcePath, persistencePath);
            sut.PatcherNameSanitizer.Received(1).Sanitize(patcher.Name);
        }
        
        [Theory, SynthAutoData]
        public void OutputPathUnderWorkingDirectory(
            IPatcherRun patcher,
            int key,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath,
            RunArgsConstructor sut)
        {
            var result = sut.GetArgs(patcher, key, outputKey, sourcePath, persistencePath);
            result.OutputPath.IsUnderneath(sut.ProfileDirectories.WorkingDirectory)
                .Should().BeTrue();
        }
        
        [Theory, SynthAutoData]
        public void OutputPathShouldContainKey(
            IPatcherRun patcher,
            int key,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath,
            RunArgsConstructor sut)
        {
            var result = sut.GetArgs(patcher, key, outputKey, sourcePath, persistencePath);
            result.OutputPath.Name.String.Should().Contain(key.ToString());
        }
        
        [Theory, SynthAutoData]
        public void OutputPathShouldContainsSanitizedName(
            IPatcherRun patcher,
            int key,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath,
            string sanitize,
            RunArgsConstructor sut)
        {
            sut.PatcherNameSanitizer.Sanitize(default!).ReturnsForAnyArgs(sanitize);
            var result = sut.GetArgs(patcher, key, outputKey, sourcePath, persistencePath);
            result.OutputPath.Name.String.Should().Contain(sanitize);
        }
        
        [Theory, SynthAutoData]
        public void PatcherNameShouldBeSanitizedName(
            IPatcherRun patcher,
            int key,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath,
            string sanitize,
            RunArgsConstructor sut)
        {
            sut.PatcherNameSanitizer.Sanitize(default!).ReturnsForAnyArgs(sanitize);
            var result = sut.GetArgs(patcher, key, outputKey, sourcePath, persistencePath);
            result.PatcherName.Should().Be(sanitize);
        }
        
        [Theory, SynthAutoData]
        public void OutputPathShouldNotContainOriginalName(
            IPatcherRun patcher,
            int key,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath,
            string sanitize,
            RunArgsConstructor sut)
        {
            sut.PatcherNameSanitizer.Sanitize(default!).ReturnsForAnyArgs(sanitize);
            var result = sut.GetArgs(patcher, key, outputKey, sourcePath, persistencePath);
            result.OutputPath.Name.String.Should().NotContain(patcher.Name);
        }
        
        [Theory, SynthAutoData]
        public void TypicalPassalong(
            IPatcherRun patcher,
            int key,
            ModKey outputKey,
            FilePath sourcePath,
            string? persistencePath,
            DirectoryPath dataDir,
            GameRelease release,
            FilePath loadOrderPath,
            RunArgsConstructor sut)
        {
            sut.DataDirectoryProvider.Path.Returns(dataDir);
            sut.ReleaseContext.Release.Returns(release);
            sut.RunLoadOrderPathProvider.Path.Returns(loadOrderPath);
            var result = sut.GetArgs(patcher, key, outputKey, sourcePath, persistencePath);
            result.SourcePath.Should().Be(sourcePath);
            result.DataFolderPath.Should().Be(dataDir);
            result.LoadOrderFilePath.Should().Be(loadOrderPath);
        }
    }
}