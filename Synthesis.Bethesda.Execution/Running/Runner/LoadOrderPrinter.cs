using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface ILoadOrderPrinter
{
    void Print(IEnumerable<ILoadOrderListingGetter> loadOrder);
}

public class LoadOrderPrinter : ILoadOrderPrinter
{
    private readonly ILogger _logger;

    public LoadOrderPrinter(ILogger logger)
    {
        _logger = logger;
    }
        
    public void Print(IEnumerable<ILoadOrderListingGetter> loadOrder)
    {
        _logger.Information("Load Order:");
        loadOrder.WithIndex().ForEach(i => _logger.Information($" [{i.Index,3}] {i.Item}"));
        _logger.Information("Remaining load order after the patch was trimmed");
    }
}