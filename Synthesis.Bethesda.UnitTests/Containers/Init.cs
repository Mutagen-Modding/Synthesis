using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Autofac;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;
using Synthesis.Bethesda.GUI.Views;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Containers
{
    public class Init
    {
        [Fact]
        public void GitInitPatcherVm()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterModule<GuiGitPatcherModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterMock<IPatcherIdProvider>();
            builder.RegisterMock<GithubPatcherSettings>();
            builder.RegisterMock<ISynthesisProfileSettings>();
            builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
                .As<IProfileIdentifier>()
                .As<IGameReleaseContext>();
            var cont = builder.Build();
            cont.Validate(
                typeof(GitPatcherInitVm));
        }
        
        [Fact]
        public void SolutionPatcherInitVm()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterModule<GuiSolutionPatcherModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterMock<IPatcherIdProvider>();
            builder.RegisterMock<ISynthesisProfileSettings>();
            builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
                .As<IProfileIdentifier>()
                .As<IGameReleaseContext>();
            var cont = builder.Build();
            cont.Validate(
                typeof(SolutionPatcherInitVm));
        }
        
        [Fact]
        public void CliPatcherInitVm()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterModule<GuiCliModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterMock<IPatcherIdProvider>();
            builder.RegisterMock<CliPatcherSettings>();
            builder.RegisterMock<ISynthesisProfileSettings>();
            builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
                .As<IProfileIdentifier>()
                .As<IGameReleaseContext>();
            var cont = builder.Build();
            cont.Validate(
                typeof(CliPatcherInitVm));
        }
    }
}