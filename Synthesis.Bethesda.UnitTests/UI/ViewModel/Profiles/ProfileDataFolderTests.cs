using System.IO.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Installs;
using Noggog;
using Noggog.Reactive;
using NSubstitute;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.GUI;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.ViewModel.Profiles
{
    public class ProfileDataFolderTests : IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public ProfileDataFolderTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Theory]
        [ClassData(typeof(ReturnData))]
        public async Task HasDataPathOverride(Utility.Return r)
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var folder = new DirectoryPath("SomeFolder");
            fileSystem.Directory.Exists(folder.Path).Returns(r);
            var locator = new ProfileDataFolder(
                _Fixture.Inject.Create<ILogger>(),
                _Fixture.Inject.Create<ISchedulerProvider>(),
                _Fixture.Inject.Create<IWatchDirectory>(),
                fileSystem,
                _Fixture.Inject.Create<IGameDirectoryLookup>(),
                _Fixture.Inject.Create<IProfileIdentifier>());
            locator.DataPathOverride = folder;
                
            locator.DataFolderResult.Succeeded.Should().Be(r == Utility.Return.True);
            if (r == Utility.Return.True)
            {
                locator.DataFolderResult.Value.Should().Be(folder);
                locator.Path.Path.Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                locator.Path.Path.Should().BeNullOrWhiteSpace();
            }
        }

        [Theory]
        [ClassData(typeof(ReturnData))]
        public void GameLocation(Utility.Return r)
        {
            var ident = _Fixture.Inject.Create<IProfileIdentifier>();
            var gameLocator = Substitute.For<IGameDirectoryLookup>();
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
            gameLocator.TryGet(ident.Release, out Arg.Any<DirectoryPath>())
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
            var locator = new ProfileDataFolder(
                _Fixture.Inject.Create<ILogger>(),
                _Fixture.Inject.Create<ISchedulerProvider>(),
                _Fixture.Inject.Create<IWatchDirectory>(),
                fileSystem,
                gameLocator,
                ident);
            locator.DataFolderResult.Succeeded.Should().Be(r == Utility.Return.True);
            if (r == Utility.Return.True)
            {
                locator.Path.Path.Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                locator.Path.Path.Should().BeNullOrWhiteSpace();
            }
        }

        [Theory]
        [ClassData(typeof(ReturnData))]
        public void WatchesForFileExistence(Utility.Return r)
        {
            var ident = _Fixture.Inject.Create<IProfileIdentifier>();
            var gameLocator = Substitute.For<IGameDirectoryLookup>();
            var folder = new DirectoryPath("SomeFolder");
            gameLocator.TryGet(ident.Release, out Arg.Any<DirectoryPath>())
                .Returns(x =>
                {
                    x[1] = folder;
                    return true;
                });
            var fileSystem = Substitute.For<IFileSystem>();
            var dataFolder = Path.Combine(folder.Path, "Data");
            fileSystem.Directory.Exists(dataFolder).Returns(r);
            var watchFile = new WatchDirectory(fileSystem);
            var locator = new ProfileDataFolder(
                _Fixture.Inject.Create<ILogger>(),
                _Fixture.Inject.Create<ISchedulerProvider>(),
                watchFile,
                fileSystem,
                gameLocator,
                ident);
                
            locator.DataFolderResult.Succeeded.Should().Be(r == Utility.Return.True);
            if (r == Utility.Return.True)
            {
                locator.DataFolderResult.Value.Path.Should().Be(dataFolder);
                locator.Path.Path.Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                locator.Path.Path.Should().BeNullOrWhiteSpace();
            }
        }
    }
}