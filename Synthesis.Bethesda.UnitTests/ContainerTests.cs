using System.ComponentModel;
using System.Windows;
using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Autofac;
using NSubstitute;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.GUI;
using Synthesis.Bethesda.GUI.DI;
using Synthesis.Bethesda.GUI.Services.Startup;
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
        public void GuiMainModule()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            var cont = builder.Build();
            cont.ValidateRegistrations(typeof(IStartup));
        }
        
        [Fact]
        public void ProfileModule()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterModule(new ProfileModule(Substitute.For<IProfileIdentifier>()));
            builder.RegisterMock<IMainWindow>();
            builder.RegisterMock<IWindowPlacement>();
            builder.RegisterMock<IGameReleaseContext>();
            var cont = builder.Build();
            cont.ValidateRegistrations(typeof(ProfileVM));
        }
        
        [Fact]
        public void CliModule()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<Synthesis.Bethesda.CLI.MainModule>();
            var cont = builder.Build();
            cont.ValidateRegistrations(typeof(IRunPatcherPipeline));
        }
        
        [Fact]
        public void ImpactModule()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<Synthesis.Bethesda.ImpactTester.MainModule>();
            var cont = builder.Build();
            cont.ValidateRegistrations(typeof(IBuild));
        }
    }
}