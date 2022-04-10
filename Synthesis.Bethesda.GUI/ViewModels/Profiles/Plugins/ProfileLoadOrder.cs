using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins
{
    public interface IProfileLoadOrder
    {
        IObservableList<ReadOnlyModListingVM> LoadOrder { get; }
        ErrorResponse State { get; }
    }

    public class ProfileLoadOrder : ViewModel, IProfileLoadOrder, IProfileLoadOrderProvider
    {
        public IObservableList<ReadOnlyModListingVM> LoadOrder { get; }

        private readonly ObservableAsPropertyHelper<ErrorResponse> _state;
        public ErrorResponse State => _state.Value;

        public ProfileLoadOrder(
            ILogger logger,
            ILiveLoadOrderProvider liveLoadOrderProvider,
            IPluginListingsPathProvider listingsPathProvider,
            ICreationClubListingsPathProvider cccLstingsPathProvider,
            IProfileIdentifier ident,
            IProfileDataFolderVm dataFolder)
        {
            var loadOrderResult = dataFolder.DataFolderResult
                .DistinctUntilChanged()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    if (x.Failed)
                    {
                        return (Results: Observable.Empty<IChangeSet<ReadOnlyModListingVM>>(), State: Observable.Return(ErrorResponse.Fail("Data folder not set")));
                    }
                    logger.Error("Getting live load order for {Release}. DataDirectory: {DataDirectory}, Plugin File Path: {PluginFilePath}, CCC Plugin File Path: {CccPluginFilePath}", ident.Release, x.Value, listingsPathProvider.Path, cccLstingsPathProvider.Path);
                    var liveLo = liveLoadOrderProvider.Get(out var errors)
                        .Transform(listing => new ReadOnlyModListingVM(listing, x.Value))
                        .DisposeMany();
                    return (Results: liveLo, State: errors);
                })
                .StartWith((Results: Observable.Empty<IChangeSet<ReadOnlyModListingVM>>(), State: Observable.Return(ErrorResponse.Fail("Load order uninitialized"))))
                .Replay(1)
                .RefCount();

            LoadOrder = loadOrderResult
                .Select(x => x.Results)
                .Switch()
                .ObserveOnGui()
                .AsObservableList();

            _state = loadOrderResult
                .Select(x => x.State)
                .Switch()
                .ToGuiProperty(this, nameof(State), ErrorResponse.Success, deferSubscription: true);
            
            loadOrderResult.Select(lo => lo.State)
                // Skip the uninitialized state
                .Skip(1)
                .Switch()
                .Subscribe(loErr =>
                {
                    if (loErr.Succeeded)
                    {
                        logger.Information("Load order location successful");
                    }
                    else
                    {
                        logger.Information("Load order location error: {Reason}", loErr.Reason);
                    }
                })
                .DisposeWith(this);
        }

        public IEnumerable<IModListingGetter> Get()
        {
            return LoadOrder.Items.Select<ReadOnlyModListingVM, IModListingGetter>(x => x);
        }
    }
}