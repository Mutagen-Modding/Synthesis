using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;

namespace Synthesis.Bethesda.UnitTests.Containers;

public class InitTests
{
    [Fact]
    public void GitInitPatcherVm()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<MainModule>();
        builder.RegisterModule<GuiGitPatcherModule>();
        ContainerTestUtil.RegisterCommonMocks(builder);
        builder.RegisterMock<IPatcherIdProvider>();
        builder.RegisterMock<GithubPatcherSettings>();
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<IGameReleaseContext>();
        builder.RegisterMock<IPipelineSettingsPath>();
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
        ContainerTestUtil.RegisterCommonMocks(builder);
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<IGameReleaseContext>();
        builder.RegisterMock<IPipelineSettingsPath>();
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
        ContainerTestUtil.RegisterCommonMocks(builder);
        builder.RegisterMock<IPatcherIdProvider>();
        builder.RegisterMock<CliPatcherSettings>();
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<IGameReleaseContext>();
        builder.RegisterMock<IPipelineSettingsPath>();
        var cont = builder.Build();
        cont.Validate(
            typeof(CliPatcherInitVm));
    }
}