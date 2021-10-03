using System.Collections.Generic;
using Autofac;
using FluentAssertions;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Autofac;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.Views;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Containers
{
    public class Gui
    {
        private static ContainerBuilder GetBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGithubPatcherIdentifier>();
            builder.RegisterMock<ISynthesisProfileSettings>();
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
}