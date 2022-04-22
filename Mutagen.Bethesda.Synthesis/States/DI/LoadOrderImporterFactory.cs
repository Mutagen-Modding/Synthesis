using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Records.DI;

namespace Mutagen.Bethesda.Synthesis.States.DI;

public interface ILoadOrderImporterFactory
{
    ILoadOrderImporter<TModGetter> Get<TModGetter> (
        string dataFolder,
        IEnumerable<IModListingGetter> listings,
        GameRelease release)
        where TModGetter : class, IModGetter;
}

public class LoadOrderImporterFactory : ILoadOrderImporterFactory
{
    private readonly IFileSystem _FileSystem;

    public LoadOrderImporterFactory(IFileSystem fileSystem)
    {
        _FileSystem = fileSystem;
    }
        
    public ILoadOrderImporter<TModGetter> Get<TModGetter>(
        string dataFolder,
        IEnumerable<IModListingGetter> listings,
        GameRelease release)
        where TModGetter : class, IModGetter
    {
        return new LoadOrderImporter<TModGetter>(
            _FileSystem,
            new DataDirectoryInjection(dataFolder),
            new LoadOrderListingsInjection(listings),
            new ModImporter<TModGetter>(
                _FileSystem,
                new GameReleaseInjection(release)));
    }
}