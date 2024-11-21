using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.UnitTests.Containers;

public class MainGuiTests
{
    private static ContainerBuilder GetBuilder()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<MainModule>();
        ContainerTestUtil.RegisterCommonMocks(builder);
        builder.RegisterMock<IProfileIdentifier>();
        builder.RegisterMock<IGameReleaseContext>();
        builder.RegisterMock<IPipelineSettingsPath>();
        return builder;
    }
        
    [Fact]
    public void GuiModule()
    {
        var builder = GetBuilder();
        var cont = builder.Build();
        cont.Validate(
            typeof(IStartup), 
            typeof(ProfileVm), 
            typeof(RunVm));
    }
}