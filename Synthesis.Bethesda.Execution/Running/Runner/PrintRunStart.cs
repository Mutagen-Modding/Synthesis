using System.Collections.Generic;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IPrintRunStart
{
    void Print(IEnumerable<IGroupRun> groups);
}

public class PrintRunStart : IPrintRunStart
{
    private readonly ILogger _logger;

    public PrintRunStart(ILogger logger)
    {
        _logger = logger;
    }
        
    public void Print(IEnumerable<IGroupRun> groups)
    {
        _logger.Information("================= Starting Pipeline Run =================");
        foreach (var group in groups)
        {
            _logger.Information("  {Group}", group.ModKey.Name);
            foreach (var patcher in group.Patchers)
            {
                _logger.Information("    {Patcher}", patcher.Run.Name);
            }
        }
    }
}