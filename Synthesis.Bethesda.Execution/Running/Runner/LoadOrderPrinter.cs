using System.Collections.Generic;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface ILoadOrderPrinter
    {
        void Print(IEnumerable<IModListingGetter> loadOrder);
    }

    public class LoadOrderPrinter : ILoadOrderPrinter
    {
        private readonly IRunReporter _reporter;

        public LoadOrderPrinter(
            IRunReporter reporter)
        {
            _reporter = reporter;
        }
        
        public void Print(IEnumerable<IModListingGetter> loadOrder)
        {
            _reporter.WriteOverall("Load Order:");
            loadOrder.WithIndex().ForEach(i => _reporter.WriteOverall($" [{i.Index,3}] {i.Item}"));
        }
    }
}