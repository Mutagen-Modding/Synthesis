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

    public PatcherStateFactory GetStateFactory()
    {
        return new PatcherStateFactory(
            FileSystem,
            new LoadOrderImporterFactory(
                FileSystem,
                new MasterFlagsLookupProvider(
                    new GameReleaseInjection(Release),
                    FileSystem,
                    new DataDirectoryInjection(DataFolder))),
            GetStateLoadOrder());
    }

    public GetStateLoadOrder GetStateLoadOrder()
    {
        var gameReleaseInjection = new GameReleaseInjection(Release);
        var categoryContext = new GameCategoryContext(gameReleaseInjection);
        var dataDirectoryInjection = new DataDirectoryInjection(DataFolder);
        var gameLoc = new GameLocatorLookupCache();
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
                        gameLoc)),
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
                    new PluginListingCommentTrimmer(),
                    new LoadOrderListingParser(
                        new HasEnabledMarkersProvider(
                            new GameReleaseInjection(Release))))));
    }
}