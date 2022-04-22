using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface ILoadOrderPrinter
{
    void Print(IEnumerable<IModListingGetter> loadOrder);
}

public class LoadOrderPrinter : ILoadOrderPrinter
{
    private readonly ILogger _logger;

    public LoadOrderPrinter(ILogger logger)
    {
        _logger = logger;
    }
        
    public void Print(IEnumerable<IModListingGetter> loadOrder)
    {
        _logger.Information("Load Order:");
        loadOrder.WithIndex().ForEach(i => _logger.Information($" [{i.Index,3}] {i.Item}"));
    }
}