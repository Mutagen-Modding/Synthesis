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

namespace Synthesis.Bethesda.UnitTests.UI.ViewModel.Profiles;

public class ProfileDataFolderTests
{
    [Theory]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.True, UseMockFileSystem: false)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.False, UseMockFileSystem: false)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.Throw, UseMockFileSystem: false)]
    public async Task HasDataPathOverride(
        Utility.Return r,
        DirectoryPath folder,
        ProfileDataFolderVm sut)
    {
        sut.FileSystem.Directory.Exists(folder.Path).Returns(r);
        sut.DataPathOverride = folder;
                
        GetResponse<DirectoryPath> result = GetResponse<DirectoryPath>.Failure;
        sut.DataFolderResult.Subscribe(x => result = x);
            
        result.Succeeded.Should().Be(r == Utility.Return.True);
        if (r == Utility.Return.True)
        {
            result.Value.Should().Be(folder);
            sut.Path.Path.Should().NotBeNullOrWhiteSpace();
        }
        else
        {
            sut.Path.Path.Should().BeNullOrWhiteSpace();
        }
    }

    [Theory]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.True, UseMockFileSystem: false, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.False, UseMockFileSystem: false, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.Throw, UseMockFileSystem: false, OmitAutoProperties: true)]
    public void GameLocation(
        Utility.Return r,
        ProfileDataFolderVm sut)
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
            
        GetResponse<DirectoryPath> result = GetResponse<DirectoryPath>.Failure;
        sut.DataFolderResult.Subscribe(x => result = x);
            
        result.Succeeded.Should().Be(r == Utility.Return.True);
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
    [SynthCustomInlineData(ExtraParameters: Utility.Return.True, UseMockFileSystem: false, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.False, UseMockFileSystem: false, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.Throw, UseMockFileSystem: false, OmitAutoProperties: true)]
    public void WatchesForFileExistence(
        Utility.Return r,
        DirectoryPath folder,
        [Frozen]IFileSystem fs,
        [Frozen]IProfileIdentifier ident,
        [Frozen]IGameDirectoryLookup lookup,
        Func<ProfileDataFolderVm> sutF)
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
            
        GetResponse<DirectoryPath> result = GetResponse<DirectoryPath>.Failure;
        sut.DataFolderResult.Subscribe(x => result = x);
                
        result.Succeeded.Should().Be(r == Utility.Return.True);
        if (r == Utility.Return.True)
        {
            result.Value.Path.Should().Be(dataFolder);
            sut.Path.Path.Should().NotBeNullOrWhiteSpace();
        }
        else
        {
            sut.Path.Path.Should().BeNullOrWhiteSpace();
        }
    }
}