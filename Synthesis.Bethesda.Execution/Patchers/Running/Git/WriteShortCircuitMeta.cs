using System.IO.Abstractions;
using Newtonsoft.Json;
using Serilog;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IWriteShortCircuitMeta
{
    void WriteMeta(string metaPath, GitCompilationMeta meta);
}

public class WriteShortCircuitMeta : IWriteShortCircuitMeta
{
    private readonly IFileSystem _fs;
    private readonly ILogger _logger;

    public WriteShortCircuitMeta(
        IFileSystem fs, 
        ILogger logger)
    {
        _fs = fs;
        _logger = logger;
    }
    
    public void WriteMeta(string metaPath, GitCompilationMeta meta)
    {
        _logger.Information("Writing compilation meta path: {Path}.  Settings: {Settings}", metaPath, meta);
        _fs.File.WriteAllText(
            metaPath,
            JsonConvert.SerializeObject(meta));
    }
}