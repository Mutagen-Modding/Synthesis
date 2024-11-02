using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis.Profiles;
using Serilog;
using StrongInject;
using Synthesis.Bethesda.CLI.Common;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.CLI.CreateProfileCli;

[Register(typeof(PipelineSettingsPath), typeof(IPipelineSettingsPath))]
[Register(typeof(PipelineSettingsV2Reader), typeof(IPipelineSettingsV2Reader))]
[Register(typeof(PipelineSettingsExporter), typeof(IPipelineSettingsExporter))]
[Register(typeof(PipelineSettingsModifier))]
[Register(typeof(CreateProfileId))]
[Register(typeof(CreateProfileRunner))]
public partial class CreateProfileRunnerContainer : IContainer<CreateProfileRunner>
{
    [Instance] private readonly IFileSystem _fileSystem;
    [Instance] private readonly ILogger _logger;
    [Instance] private readonly IGameReleaseContext _releaseContext;

    public CreateProfileRunnerContainer(IFileSystem fileSystem, ILogger logger, IGameReleaseContext releaseContext)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _releaseContext = releaseContext;
    }
}