using System.ComponentModel;
using System.Windows;
using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Noggog.Autofac;
using NSubstitute;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.Views;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class Test : IMainWindow
    {
        public Visibility Visibility { get; set; }
        public object DataContext { get; set; } = null!;
        public event CancelEventHandler? Closing;
    }
    
    public class ContainerTests
    {
        [Fact]
        public void GuiModule()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
                .As<IProfileIdentifier>()
                .As<IProfileNameProvider>()
                .As<IGameReleaseContext>();
            var cont = builder.Build();
            cont.Validate(
                typeof(IStartup), 
                typeof(ProfileVm), 
                typeof(PatchersRunVm));
        }
        
        [Fact]
        public void GitPatcherVm()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<GitPatcherModule>();
            builder.RegisterModule<MainModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterMock<IPatcherIdProvider>();
            builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
                .As<IProfileIdentifier>()
                .As<IProfileNameProvider>()
                .As<IGameReleaseContext>();
            var cont = builder.Build();
            cont.Validate(
                typeof(GitPatcherVm),
                typeof(GitPatcherInitVm));
        }
        
        [Fact]
        public void SolutionPatcherVm()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<SolutionPatcherModule>();
            builder.RegisterModule<MainModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterMock<IPatcherIdProvider>();
            builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
                .As<IProfileIdentifier>()
                .As<IProfileNameProvider>()
                .As<IGameReleaseContext>();
            var cont = builder.Build();
            cont.Validate(
                typeof(SolutionPatcherVm),
                typeof(SolutionPatcherInitVm));
        }
        
        [Fact]
        public void CliPatcherVm()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<CliPatcherModule>();
            builder.RegisterModule<MainModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterMock<IPatcherIdProvider>();
            builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
                .As<IProfileIdentifier>()
                .As<IProfileNameProvider>()
                .As<IGameReleaseContext>();
            var cont = builder.Build();
            cont.Validate(
                typeof(CliPatcherVm),
                typeof(CliPatcherInitVm));
        }
        
        [Fact]
        public void CliModule()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new Synthesis.Bethesda.CLI.MainModule(
                    new RunPatcherPipelineInstructions()));
            var cont = builder.Build();
            cont.Validate(typeof(IRunPatcherPipeline));
        }
        
        [Fact]
        public void ImpactModule()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<Synthesis.Bethesda.ImpactTester.MainModule>();
            var cont = builder.Build();
            cont.Validate(typeof(IBuild));
        }
    }
}