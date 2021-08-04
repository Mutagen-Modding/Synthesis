using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order.DI;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunLoadOrderPreparer
    {
        void Write(ModPath outputPath);
    }

    public class RunLoadOrderPreparer : IRunLoadOrderPreparer
    {
        public ILoadOrderForRunProvider LoadOrderForRunProvider { get; }
        public ILoadOrderPrinter Printer { get; }
        public IRunLoadOrderPathProvider LoadOrderPathProvider { get; }
        public ILoadOrderWriter LoadOrderWriter { get; }

        public RunLoadOrderPreparer(
            ILoadOrderForRunProvider loadOrderForRunProvider,
            ILoadOrderPrinter printer,
            IRunLoadOrderPathProvider runLoadOrderPathProvider,
            ILoadOrderWriter loadOrderWriter)
        {
            LoadOrderForRunProvider = loadOrderForRunProvider;
            Printer = printer;
            LoadOrderPathProvider = runLoadOrderPathProvider;
            LoadOrderWriter = loadOrderWriter;
        }

        public void Write(ModPath outputPath)
        {
            var loadOrderList = LoadOrderForRunProvider.Get(outputPath);
            
            Printer.Print(loadOrderList);
            
            LoadOrderWriter.Write(
                LoadOrderPathProvider.Path,
                loadOrderList,
                removeImplicitMods: true);
        }
    }
}