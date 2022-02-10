using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Noggog.Autofac;
using NSubstitute;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Containers;

public class MainGuiTests
{
    private static ContainerBuilder GetBuilder()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<MainModule>();
        ContainerTestUtil.RegisterCommonMocks(builder);
        builder.RegisterInstance(Substitute.For<IProfileIdentifier>())
            .As<IProfileIdentifier>()
            .As<IGameReleaseContext>();
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