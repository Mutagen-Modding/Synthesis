using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Noggog;
using System;
using Noggog.WPF;
using ReactiveUI;
using Serilog;

namespace Synthesis.Bethesda.GUI.Profiles.Plugins
{
    public interface IProfileLoadOrder
    {
        IObservableList<ReadOnlyModListingVM> LoadOrder { get; }
        ErrorResponse State { get; }
    }

    public class ProfileLoadOrder : ViewModel, IProfileLoadOrder
    {
        public IObservableList<ReadOnlyModListingVM> LoadOrder { get; }

        private readonly ObservableAsPropertyHelper<ErrorResponse> _State;
        public ErrorResponse State => _State.Value;

        public ProfileLoadOrder(
            ILogger logger,
            IProfileIdentifier ident,
            IProfileDataFolder dataFolder)
        {

            var loadOrderResult = dataFolder.DataFolderResult
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    if (x.Failed)
                    {
                        return (Results: Observable.Empty<IChangeSet<ReadOnlyModListingVM>>(), State: Observable.Return(ErrorResponse.Fail("Data folder not set")));
                    }
                    logger.Error($"Getting live load order for {ident.Release} -> {x.Value}");
                    var liveLo = Mutagen.Bethesda.Plugins.Order.LoadOrder.GetLiveLoadOrder(ident.Release, x.Value, out var errors)
                        .Transform(listing => new ReadOnlyModListingVM(listing, x.Value))
                        .DisposeMany();
                    return (Results: liveLo, State: errors);
                })
                .StartWith((Results: Observable.Empty<IChangeSet<ReadOnlyModListingVM>>(), State: Observable.Return(ErrorResponse.Fail("Load order uninitialized"))))
                .Replay(1)
                .RefCount();
            
            loadOrderResult.Select(lo => lo.State)
                .Switch()
                .Subscribe(loErr =>
                {
                    if (loErr.Succeeded)
                    {
                        logger.Information($"Load order location successful");
                    }
                    else
                    {
                        logger.Information($"Load order location error: {loErr.Reason}");
                    }
                })
                .DisposeWith(this);

            LoadOrder = loadOrderResult
                .Select(x => x.Results)
                .Switch()
                .AsObservableList();

            _State = loadOrderResult
                .Select(x => x.State)
                .Switch()
                .ToGuiProperty(this, nameof(State), ErrorResponse.Success);
        }
    }
}