using System.Collections.Generic;
using Path = System.IO.Path;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IProvideTemporaryLoadOrder
    {
        TempFile Get(IEnumerable<IModListingGetter> loadOrder);
    }

    public class ProvideTemporaryLoadOrder : IProvideTemporaryLoadOrder
    {
        private readonly ILoadOrderWriter _loadOrderWriter;
        private readonly IProvideWorkingDirectory _paths;

        public ProvideTemporaryLoadOrder(
            ILoadOrderWriter loadOrderWriter,
            IProvideWorkingDirectory paths)
        {
            _loadOrderWriter = loadOrderWriter;
            _paths = paths;
        }
        
        public TempFile Get(IEnumerable<IModListingGetter> loadOrder)
        {
            var loadOrderFile = new TempFile(
                Path.Combine(_paths.WorkingDirectory, "RunnabilityChecks", Path.GetRandomFileName()));

            _loadOrderWriter.Write(
                loadOrderFile.File.Path,
                loadOrder,
                removeImplicitMods: true);

            return loadOrderFile;
        }
    }
}