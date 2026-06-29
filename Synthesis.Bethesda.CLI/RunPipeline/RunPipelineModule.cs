using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Synthesis.Projects;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPipelineModule : Module
{
    private readonly IFileSystem _fileSystem;
    private readonly RunPatcherPipelineCommand _cmd;

    public RunPipelineModule(
        IFileSystem fileSystem,
        RunPatcherPipelineCommand cmd)
    {
        _fileSystem = fileSystem;
        _cmd = cmd;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(new RunPipelineCliModule(_fileSystem, _cmd));
    }
}