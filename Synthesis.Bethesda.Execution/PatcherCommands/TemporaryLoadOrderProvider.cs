using System.Collections.Generic;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Utility;
using Path = System.IO.Path;

namespace Synthesis.Bethesda.Execution.PatcherCommands
{
    public interface ITemporaryLoadOrderProvider
    {
        ITempFile Get(IEnumerable<IModListingGetter> loadOrder);
    }

    public class TemporaryLoadOrderProvider : ITemporaryLoadOrderProvider
    {
        public const string Folder = "TemporaryRuns";
        
        public ITempFileProvider TempFileProvider { get; }
        public ILoadOrderWriter LoadOrderWriter { get; }
        public IRandomFileNameProvider RandomFileNameProvider { get; }
        public IWorkingDirectoryProvider Paths { get; }

        public TemporaryLoadOrderProvider(
            ITempFileProvider tempFileProvider,
            ILoadOrderWriter loadOrderWriter,
            IWorkingDirectoryProvider paths,
            IRandomFileNameProvider randomFileNameProvider)
        {
            TempFileProvider = tempFileProvider;
            LoadOrderWriter = loadOrderWriter;
            RandomFileNameProvider = randomFileNameProvider;
            Paths = paths;
        }
        
        public ITempFile Get(IEnumerable<IModListingGetter> loadOrder)
        {
            var loadOrderFile = TempFileProvider.Create(
                Path.Combine(Paths.WorkingDirectory, Folder, RandomFileNameProvider.Get()));

            LoadOrderWriter.Write(
                loadOrderFile.File.Path,
                loadOrder,
                removeImplicitMods: true);

            return loadOrderFile;
        }
    }
}