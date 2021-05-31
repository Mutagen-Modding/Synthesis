using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Synthesis.Bethesda;
using System.Collections.Generic;

namespace Mutagen.Bethesda.Synthesis
{
    /// <summary>
    /// A class housing all the tools, parameters, and entry points for a typical Synthesis check runnability analysis
    /// </summary>
    public class RunnabilityState : IRunnabilityState
    {
        /// <summary>
        /// Instructions given to the patcher from the Synthesis pipeline
        /// </summary>
        public CheckRunnability Settings { get; }

        /// <summary>
        /// Current Load Order 
        /// </summary>
        public ILoadOrderGetter<IModListingGetter> LoadOrder { get; }

        public FilePath LoadOrderFilePath => Settings.LoadOrderFilePath;

        public DirectoryPath DataFolderPath => Settings.DataFolderPath;

        public GameRelease GameRelease => Settings.GameRelease;

        public RunnabilityState(
            CheckRunnability settings,
            ILoadOrderGetter<IModListingGetter> loadOrder)
        {
            Settings = settings;
            LoadOrder = loadOrder;
        }

        public GameEnvironmentState<TModSetter, TModGetter> GetEnvironmentState<TModSetter, TModGetter>()
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
        {
            var lo = Plugins.Order.LoadOrder.Import<TModGetter>(DataFolderPath, LoadOrder.ListedOrder, GameRelease);
            return new GameEnvironmentState<TModSetter, TModGetter>(
                dataFolderPath: DataFolderPath,
                loadOrderFilePath: LoadOrderFilePath,
                creationKitLoadOrderFilePath: null,
                loadOrder: lo,
                linkCache: lo.ToImmutableLinkCache<TModSetter, TModGetter>());
        }
    }
}
