using Autofac;
using Noggog;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Running.Cli;
using Synthesis.Bethesda.Execution.Running.Cli.Settings;
using Synthesis.Bethesda.Execution.Settings;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Containers
{
    public class Cli
    {
        [Fact]
        public void ProfileLocator()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new Synthesis.Bethesda.CLI.MainModule(
                    new RunPatcherPipelineInstructions()));
            var cont = builder.Build();
            cont.Validate(typeof(IRunProfileProvider));
        }
        
        [Fact]
        public void CliRun()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new Synthesis.Bethesda.CLI.MainModule(
                    new RunPatcherPipelineInstructions()));
            builder.RegisterMock<IProfileIdentifier>();
            var cont = builder.Build();
            cont.Validate(typeof(IRunPatcherPipeline));
        }

        [Fact]
        public void CliPatcher()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new Synthesis.Bethesda.CLI.MainModule(
                    new RunPatcherPipelineInstructions()));
            builder.RegisterModule<PatcherModule>();
            builder.RegisterMock<IProfileIdentifier>();
            builder.RegisterMock<CliPatcherSettings>()
                .As<IPathToExecutableInputProvider>()
                .As<IPatcherNameProvider>();
            var cont = builder.Build();
            cont.Validate(typeof(ICliPatcherRun));
        }

        [Fact]
        public void SlnPatcher()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new Synthesis.Bethesda.CLI.MainModule(
                    new RunPatcherPipelineInstructions()));
            builder.RegisterModule<PatcherModule>();
            builder.RegisterMock<IProfileIdentifier>();
            builder.RegisterMock<SolutionPatcherSettings>()
                .As<IPatcherNameProvider>()
                .As<IPathToSolutionFileProvider>()
                .As<IProjectSubpathProvider>();
            var cont = builder.Build();
            cont.Validate(typeof(ISolutionPatcherRun));
        }

        [Fact]
        public void GitPatcher()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new Synthesis.Bethesda.CLI.MainModule(
                    new RunPatcherPipelineInstructions()));
            builder.RegisterModule<PatcherModule>();
            builder.RegisterMock<IProfileIdentifier>();
            builder.RegisterMock<GithubPatcherSettings>()
                .As<IGithubPatcherIdentifier>()
                .As<IPatcherNameProvider>();
            var cont = builder.Build();
            cont.Validate(typeof(IGitPatcherRun));
        }
    }
}