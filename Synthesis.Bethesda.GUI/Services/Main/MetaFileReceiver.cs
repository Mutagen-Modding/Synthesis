using System.IO;
using System.Reactive.Linq;
using Noggog;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.Execution.FileAssociations;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface IMetaFileReceiver
{
    IObservable<MetaFileDto> MetaFiles { get; }
}

public class MetaFileReceiver : IMetaFileReceiver
{
    public IObservable<MetaFileDto> MetaFiles { get; }
    
    public MetaFileReceiver(
        ILogger logger,
        ImportMetaDto importMetaDto,
        IWatchSingleAppArguments watchAppArgs)
    {
        MetaFiles = watchAppArgs.WatchArgs()
            .Where(x => x.Count == 1)
            .Select(x => x[0])
            .Where(x => Path.HasExtension(x) &&
                        Path.GetExtension(x).Equals(".synth", StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                try
                {
                    return importMetaDto.Import(x);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error importing meta file");
                }

                return default(MetaFileDto?);
            })
            .WhereNotNull()
            .PublishRefCount();
    }
}