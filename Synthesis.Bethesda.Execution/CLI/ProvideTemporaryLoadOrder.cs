using System.Collections.Generic;
using System.IO;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IProvideTemporaryLoadOrder
    {
        TempFile Get(GameRelease release, IEnumerable<IModListingGetter> loadOrder);
    }

    public class ProvideTemporaryLoadOrder : IProvideTemporaryLoadOrder
    {
        public TempFile Get(GameRelease release, IEnumerable<IModListingGetter> loadOrder)
        {
            var loadOrderFile = new TempFile(
                Path.Combine(Synthesis.Bethesda.Execution.Paths.WorkingDirectory, "RunnabilityChecks", Path.GetRandomFileName()));

            LoadOrder.Write(
                loadOrderFile.File.Path,
                release,
                loadOrder,
                removeImplicitMods: true);

            return loadOrderFile;
        }
    }
}