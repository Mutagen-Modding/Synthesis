using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Noggog;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Registry;

public interface IRegistryListingsProvider
{
    GetResponse<RepositoryListing[]> Get(CancellationToken cancel);
}

public class RegistryListingsProvider : IRegistryListingsProvider
{
    private readonly IFileSystem _fileSystem;
    public IRegistryListingReader ListingReader { get; }
    public IRepositoryListingFilePathProvider FilePathProvider { get; }
    public IPrepRegistryRepository PrepRegistryRepository { get; }

    public RegistryListingsProvider(
        IFileSystem fileSystem,
        IRegistryListingReader listingReader,
        IRepositoryListingFilePathProvider filePathProvider,
        IPrepRegistryRepository prepRegistryRepository)
    {
        _fileSystem = fileSystem;
        ListingReader = listingReader;
        FilePathProvider = filePathProvider;
        PrepRegistryRepository = prepRegistryRepository;
    }
        
    public GetResponse<RepositoryListing[]> Get(CancellationToken cancel)
    {
        var prepResp = PrepRegistryRepository.Prep(cancel);
        if (prepResp.Failed) return prepResp.BubbleFailure<RepositoryListing[]>();

        var listingPath = FilePathProvider.Path;
        if (!_fileSystem.File.Exists(listingPath))
        {
            return GetResponse<RepositoryListing[]>.Fail("Could not locate listing file");
        }

        var customization = ListingReader.Read(listingPath);
            
        return customization.Repositories;
    }
}