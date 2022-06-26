using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Patchers.Git.Registry;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.Registry;

public class RegistryListingsProviderTests
{
    [Theory, SynthAutoData]
    public void FailedPrepReturnsFail(
        ErrorResponse fail,
        CancellationToken cancel,
        RegistryListingsProvider sut)
    {
        sut.PrepRegistryRepository.Prep(cancel)
            .Returns(fail);
        sut.Get(cancel)
            .Succeeded.Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void MissingListingPathReturnsFailure(
        FilePath missing,
        CancellationToken cancel,
        RegistryListingsProvider sut)
    {
        sut.FilePathProvider.Path.Returns(missing);
        sut.Get(cancel)
            .Succeeded.Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void PassesListingPathToReader(
        FilePath existing,
        CancellationToken cancel,
        RegistryListingsProvider sut)
    {
        sut.FilePathProvider.Path.Returns(existing);
        sut.Get(cancel);
        sut.ListingReader.Received(1).Read(existing);
    }
        
    [Theory, SynthAutoData]
    public void ReturnsReaderResults(
        FilePath existing,
        MutagenPatchersListing listing,
        CancellationToken cancel,
        RegistryListingsProvider sut)
    {
        sut.FilePathProvider.Path.Returns(existing);
        sut.ListingReader.Read(default).ReturnsForAnyArgs(listing);
        var ret = sut.Get(cancel);
        ret.Succeeded.Should().BeTrue();
        ret.Value.Should().Equal(listing.Repositories);
    }
}