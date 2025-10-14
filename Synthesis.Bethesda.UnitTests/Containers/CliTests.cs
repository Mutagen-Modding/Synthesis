using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Noggog.Autofac;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.UnitTests.Containers;

public class CliTests
{
    [Theory, MutagenAutoData]
    public void CliRun(
        IFileSystem fileSystem,
        RunPatcherPipelineCommand cmd)
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(
            new RunPipelineModule(fileSystem, cmd));
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<ISynthesisProfileSettings>();
        var cont = builder.Build();
        cont.Validate(typeof(RunPatcherPipeline));
    }
    
    [Theory, MutagenAutoData]
    public void CliPatcher(
        IFileSystem fileSystem,
        RunPatcherPipelineCommand cmd)
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(
            new RunPipelineModule(fileSystem, cmd));
        builder.RegisterModule<PatcherModule>();
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<IProfileNameProvider>();
        builder.RegisterMock<IPatcherIdProvider>();
        builder.RegisterMock<CliPatcherSettings>()
            .As<IPathToExecutableInputProvider>()
            .As<IPatcherNameProvider>()
            .As<IPatcherNicknameProvider>();
        var cont = builder.Build();
        cont.Validate(typeof(ICliPatcherRun));
    }

    [Theory, MutagenAutoData]
    public void SlnPatcher(
        IFileSystem fileSystem,
        RunPatcherPipelineCommand cmd)
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(
            new RunPipelineModule(fileSystem, cmd));
        builder.RegisterModule<SolutionPatcherModule>();
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<IProfileNameProvider>();
        builder.RegisterMock<IPatcherIdProvider>();
        builder.RegisterMock<IDotNetPathSettingsProvider>();
        builder.RegisterMock<IShortCircuitSettingsProvider>();
        builder.RegisterMock<IExecutionParametersSettingsProvider>();
        builder.RegisterMock<IMo2CompatibilitySettingsProvider>();
        builder.RegisterMock<SolutionPatcherSettings>()
            .As<IPathToSolutionFileProvider>()
            .As<IProjectSubpathProvider>()
            .As<IPatcherNicknameProvider>();
        var cont = builder.Build();
        cont.Validate(typeof(ISolutionPatcherRun));
    }
    
    [Theory, MutagenAutoData]
    public void GitPatcher(
        IFileSystem fileSystem,
        RunPatcherPipelineCommand cmd)
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(
            new RunPipelineModule(fileSystem, cmd));
        builder.RegisterModule<GitPatcherModule>();
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<IProfileNameProvider>();
        builder.RegisterMock<IPatcherIdProvider>();
        builder.RegisterMock<IDotNetPathSettingsProvider>();
        builder.RegisterMock<IShortCircuitSettingsProvider>();
        builder.RegisterMock<IExecutionParametersSettingsProvider>();
        builder.RegisterMock<IMo2CompatibilitySettingsProvider>();
        builder.RegisterMock<GithubPatcherSettings>()
            .As<IGithubPatcherIdentifier>()
            .As<IProjectSubpathProvider>()
            .As<IPatcherNicknameProvider>();
        var cont = builder.Build();
        cont.Validate(typeof(IGitPatcherRun));
    }
}