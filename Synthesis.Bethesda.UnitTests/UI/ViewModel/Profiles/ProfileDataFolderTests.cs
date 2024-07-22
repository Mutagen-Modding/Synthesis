using System.IO.Abstractions;
using AutoFixture.Xunit2;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Testing.AutoFixture;
using NSubstitute;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.ViewModel.Profiles;

public class ProfileDataFolderTests
{
    [Theory]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.True, FileSystem: TargetFileSystem.Substitute)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.False, FileSystem: TargetFileSystem.Substitute)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.Throw, FileSystem: TargetFileSystem.Substitute)]
    public async Task HasDataPathOverride(
        Utility.Return r,
        DirectoryPath folder,
        ProfileOverridesVm sut)
    {
        sut.FileSystem.Directory.Exists(folder.Path).Returns(x =>
        {
            switch (r)
            {
                case Utility.Return.False:
                    return false;
                case Utility.Return.True:
                    return true;
                default:
                    throw new Exception();
            }
        });
        sut.DataPathOverride = folder;
                
        GetResponse<DirectoryPath> result = GetResponse<DirectoryPath>.Failure;
        sut.WhenAnyValue(x => x.DataFolderResult).Subscribe(x => result = x);
            
        result.Succeeded.Should().Be(r == Utility.Return.True);
        if (r == Utility.Return.True)
        {
            result.Value.Should().Be(folder);
            sut.DataFolderResult.Value.Path.Should().Be(folder.Path);
        }
        else if (r == Utility.Return.False)
        {
            sut.DataFolderResult.Value.Path.Should().Be(folder.Path);
        }
        else if (r == Utility.Return.Throw)
        {
            sut.DataFolderResult.Value.Path.Should().BeNullOrWhiteSpace();
        }
    }

    [Theory]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.True, FileSystem: TargetFileSystem.Substitute, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.False, FileSystem: TargetFileSystem.Substitute, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.Throw, FileSystem: TargetFileSystem.Substitute, OmitAutoProperties: true)]
    public void GameLocation(
        Utility.Return r,
        Lazy<ProfileOverridesVm> sutGetter,
        [Frozen] IFileSystem fileSystem,
        [Frozen] IDataDirectoryLookup gameLocator)
    {
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
        gameLocator.TryGet(Arg.Any<GameRelease>(), out Arg.Any<DirectoryPath>())
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

        var sut = sutGetter.Value;
            
        GetResponse<DirectoryPath> result = GetResponse<DirectoryPath>.Failure;
        sut.WhenAnyValue(x => x.DataFolderResult).Subscribe(x => result = x);
            
        result.Succeeded.Should().Be(r == Utility.Return.True);
        if (r == Utility.Return.True)
        {
            sut.DataFolderResult.Value.Path.Should().NotBeNullOrWhiteSpace();
        }
        else if (r == Utility.Return.False)
        {
            sut.DataFolderResult.Value.Path.Should().BeNullOrWhiteSpace();
        }
        else if (r == Utility.Return.Throw)
        {
            sut.DataFolderResult.Value.Path.Should().BeNullOrWhiteSpace();
        }
    }
    
    [Theory]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.True, FileSystem: TargetFileSystem.Substitute, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.False, FileSystem: TargetFileSystem.Substitute, OmitAutoProperties: true)]
    [SynthCustomInlineData(ExtraParameters: Utility.Return.Throw, FileSystem: TargetFileSystem.Substitute, OmitAutoProperties: true)]
    public void WatchesForFileExistence(
        Utility.Return r,
        DirectoryPath folder,
        [Frozen]IFileSystem fs,
        [Frozen]IProfileIdentifier ident,
        [Frozen]IDataDirectoryLookup lookup,
        Lazy<ProfileOverridesVm> sutF)
    {
        lookup.TryGet(ident.Release, out Arg.Any<DirectoryPath>())
            .Returns(x =>
            {
                x[1] = folder;
                return true;
            });
        fs.Directory.Exists(folder).Returns(r);
        var sut = sutF.Value;
            
        GetResponse<DirectoryPath> result = GetResponse<DirectoryPath>.Failure;
        sut.WhenAnyValue(x => x.DataFolderResult).Subscribe(x => result = x);
                
        result.Succeeded.Should().Be(r == Utility.Return.True);
        if (r == Utility.Return.True)
        {
            result.Value.Path.Should().Be(folder);
            sut.DataFolderResult.Value.Path.Should().NotBeNullOrWhiteSpace();
        }
        else if (r == Utility.Return.False)
        {
            sut.DataFolderResult.Value.Path.Should().NotBeNullOrWhiteSpace();
        }
        else if (r == Utility.Return.Throw)
        {
            sut.DataFolderResult.Value.Path.Should().BeNullOrWhiteSpace();
        }
    }
}