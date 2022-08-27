using System.IO.Abstractions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Installs.DI;
using Mutagen.Bethesda.Plugins.Implicit.DI;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Synthesis.States;
using Mutagen.Bethesda.Synthesis.States.DI;

namespace Synthesis.Bethesda.UnitTests;

public record TestEnvironment(
    IFileSystem FileSystem,
    GameRelease Release,
    string BaseFolder,
    string DataFolder,
    string PluginPath)
{
    public IEnumerable<ILoadOrderListingGetter> GetTypicalLoadOrder()
    {
        return StatePluginListings().Get();
    }

    public StateFactory GetStateFactory()
    {
        return new StateFactory(
            FileSystem,
            new LoadOrderImporterFactory(FileSystem),
            GetStateLoadOrder());
    }

    public GetStateLoadOrder GetStateLoadOrder()
    {
        var gameReleaseInjection = new GameReleaseInjection(Release);
        var categoryContext = new GameCategoryContext(gameReleaseInjection);
        var dataDirectoryInjection = new DataDirectoryInjection(DataFolder);
        return new GetStateLoadOrder(
            new ImplicitListingsProvider(
                FileSystem,
                dataDirectoryInjection,
                new ImplicitListingModKeyProvider(
                    gameReleaseInjection)),
            new OrderListings(),
            new CreationClubListingsProvider(
                FileSystem,
                dataDirectoryInjection,
                new CreationClubListingsPathProvider(
                    categoryContext,
                    new CreationClubEnabledProvider(categoryContext),
                    new GameDirectoryProvider(
                        gameReleaseInjection,
                        new GameLocator())),
                new CreationClubRawListingsReader()),
            StatePluginListings(),
            new EnableImplicitMastersFactory(FileSystem));
    }

    public IPluginListingsProvider StatePluginListings()
    {
        return new StatePluginsListingProvider(
            PluginPath,
            new PluginRawListingsReader(
                FileSystem,
                new PluginListingsParser(
                    new LoadOrderListingParser(
                        new HasEnabledMarkersProvider(
                            new GameReleaseInjection(Release))))));
    }
}