using System.IO.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Reactive;
using NSubstitute;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.ViewModel.Profiles
{
    public class ProfileDataFolderTests
    {
        [Theory]
        [SynthInlineData(ExtraParameters: Utility.Return.True, UseMockFileSystem: false)]
        [SynthInlineData(ExtraParameters: Utility.Return.False, UseMockFileSystem: false)]
        [SynthInlineData(ExtraParameters: Utility.Return.Throw, UseMockFileSystem: false)]
        public async Task HasDataPathOverride(
            Utility.Return r,
            ProfileDataFolder sut)
        {
            var folder = new DirectoryPath("SomeFolder");
            sut.FileSystem.Directory.Exists(folder.Path).Returns(r);
            sut.DataPathOverride = folder;
                
            sut.DataFolderResult.Succeeded.Should().Be(r == Utility.Return.True);
            if (r == Utility.Return.True)
            {
                sut.DataFolderResult.Value.Should().Be(folder);
                sut.Path.Path.Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                sut.Path.Path.Should().BeNullOrWhiteSpace();
            }
        }

        [Theory]
        [SynthInlineData(ExtraParameters: Utility.Return.True, UseMockFileSystem: false)]
        [SynthInlineData(ExtraParameters: Utility.Return.False, UseMockFileSystem: false)]
        [SynthInlineData(ExtraParameters: Utility.Return.Throw, UseMockFileSystem: false)]
        public void GameLocation(
            Utility.Return r,
            ProfileDataFolder sut)
        {
            sut.FileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
            sut.GameLocator.TryGet(Arg.Any<GameRelease>(), out Arg.Any<DirectoryPath>())
                .Returns(x =>
                {
                    switch (r)
                    {
                        case Utility.Return.False:
                            return false;
                        case Utility.Return.True:
                            x[1] = new DirectoryPath("Something");
                            return true;
                        default:
                            throw new Exception();
                    }
                });
            sut.DataFolderResult.Succeeded.Should().Be(r == Utility.Return.True);
            if (r == Utility.Return.True)
            {
                sut.Path.Path.Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                sut.Path.Path.Should().BeNullOrWhiteSpace();
            }
        }

        [Theory]
        [SynthInlineData(ExtraParameters: Utility.Return.True, UseMockFileSystem: false)]
        [SynthInlineData(ExtraParameters: Utility.Return.False, UseMockFileSystem: false)]
        [SynthInlineData(ExtraParameters: Utility.Return.Throw, UseMockFileSystem: false)]
        public void WatchesForFileExistence(
            Utility.Return r,
            DirectoryPath folder,
            [Frozen]IFileSystem fs,
            [Frozen]IProfileIdentifier ident,
            [Frozen]IGameDirectoryLookup lookup,
            Func<ProfileDataFolder> sutF)
        {
            lookup.TryGet(ident.Release, out Arg.Any<DirectoryPath>())
                .Returns(x =>
                {
                    x[1] = folder;
                    return true;
                });
            var dataFolder = Path.Combine(folder.Path, "Data");
            fs.Directory.Exists(dataFolder).Returns(r);
            var sut = sutF();
                
            sut.DataFolderResult.Succeeded.Should().Be(r == Utility.Return.True);
            if (r == Utility.Return.True)
            {
                sut.DataFolderResult.Value.Path.Should().Be(dataFolder);
                sut.Path.Path.Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                sut.Path.Path.Should().BeNullOrWhiteSpace();
            }
        }
    }
}