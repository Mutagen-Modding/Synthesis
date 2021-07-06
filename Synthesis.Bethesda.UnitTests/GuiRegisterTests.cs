using Mutagen.Bethesda.Environments.DI;
using NSubstitute;
using StructureMap;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.GUI;
using Synthesis.Bethesda.GUI.Registers;
using Synthesis.Bethesda.GUI.Views;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class GuiRegisterTests
    {
        [Fact]
        public void ValidMain()
        {
            var cont = new Container(c =>
            {
                c.IncludeRegistry<Register>();
                c.For<IMainWindow>().Use(x => Substitute.For<IMainWindow>());
                c.For<IWindowPlacement>().Use(x => Substitute.For<IWindowPlacement>());
            });
            cont.AssertConfigurationIsValid();
        }
        
        [Fact]
        public void ValidProfile()
        {
            var cont = new Container(c =>
            {
                c.IncludeRegistry<Register>();
                c.IncludeRegistry<ProfileRegister>();
                c.For<IMainWindow>().Use(x => Substitute.For<IMainWindow>());
                c.For<IWindowPlacement>().Use(x => Substitute.For<IWindowPlacement>());
                c.For<IProfileIdentifier>().Use(x => Substitute.For<IProfileIdentifier>());
                c.For<IGameReleaseContext>().Use(x => Substitute.For<IGameReleaseContext>());
            });
            cont.AssertConfigurationIsValid();
        }
    }
}