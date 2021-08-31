using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order.DI;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunLoadOrderPreparer
    {
        void Write(ModKey modKey);
    }

    public class RunLoadOrderPreparer : IRunLoadOrderPreparer
    {
        public ILoadOrderForRunProvider LoadOrderForRunProvider { get; }
        //public ILoadOrderPrinter Printer { get; }
        public IRunLoadOrderPathProvider LoadOrderPathProvider { get; }
        public ILoadOrderWriter LoadOrderWriter { get; }

        public RunLoadOrderPreparer(
            ILoadOrderForRunProvider loadOrderForRunProvider,
            //ILoadOrderPrinter printer,
            IRunLoadOrderPathProvider runLoadOrderPathProvider,
            ILoadOrderWriter loadOrderWriter)
        {
            LoadOrderForRunProvider = loadOrderForRunProvider;
            //Printer = printer;
            LoadOrderPathProvider = runLoadOrderPathProvider;
            LoadOrderWriter = loadOrderWriter;
        }

        public void Write(ModKey modKey)
        {
            var loadOrderList = LoadOrderForRunProvider.Get(modKey);
            
            //Printer.Print(loadOrderList);
            
            LoadOrderWriter.Write(
                LoadOrderPathProvider.Path,
                loadOrderList,
                removeImplicitMods: true);
        }
    }
}