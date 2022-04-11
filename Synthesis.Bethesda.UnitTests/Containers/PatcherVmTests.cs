using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Autofac;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;
using Synthesis.Bethesda.GUI.Views;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Containers;

public class PatcherVmTests
{
    [Fact]
    public void GitPatcherVm()
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
            typeof(GitPatcherVm));
    }
        
    [Fact]
    public void SolutionPatcherVm()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<MainModule>();
        builder.RegisterModule<GuiSolutionPatcherModule>();
        builder.RegisterMock<IMainWindow>();
        builder.RegisterMock<IWindowPlacement>();
        builder.RegisterMock<IGithubPatcherIdentifier>();
        builder.RegisterMock<IPatcherIdProvider>();
        builder.RegisterMock<ISynthesisProfileSettings>();
        builder.RegisterMock<IProjectSubpathDefaultSettings>();
        builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
            .As<IProfileIdentifier>()
            .As<IGameReleaseContext>();
        var cont = builder.Build();
        cont.Validate(
            typeof(SolutionPatcherVm));
    }
        
    [Fact]
    public void CliPatcherVm()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<MainModule>();
        builder.RegisterModule<GuiCliPatcherModule>();
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
            typeof(CliPatcherVm));
    }
}