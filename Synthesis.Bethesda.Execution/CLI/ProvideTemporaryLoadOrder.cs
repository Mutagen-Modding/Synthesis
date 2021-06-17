using System.Collections.Generic;
using Path = System.IO.Path;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IProvideTemporaryLoadOrder
    {
        TempFile Get(GameRelease release, IEnumerable<IModListingGetter> loadOrder);
    }

    public class ProvideTemporaryLoadOrder : IProvideTemporaryLoadOrder
    {
        private readonly IProvideWorkingDirectory _Paths;

        public ProvideTemporaryLoadOrder(
            IProvideWorkingDirectory paths)
        {
            _Paths = paths;
        }
        
        public TempFile Get(GameRelease release, IEnumerable<IModListingGetter> loadOrder)
        {
            var loadOrderFile = new TempFile(
                Path.Combine(_Paths.WorkingDirectory, "RunnabilityChecks", Path.GetRandomFileName()));

            LoadOrder.Write(
                loadOrderFile.File.Path,
                release,
                loadOrder,
                removeImplicitMods: true);

            return loadOrderFile;
        }
    }
}