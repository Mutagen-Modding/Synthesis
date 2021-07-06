using NSubstitute;
using StructureMap;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.GUI;
using Synthesis.Bethesda.GUI.Views;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class GuiRegisterTests
    {
        [Fact]
        public void ValidRegistration()
        {
            var cont = new Container(c =>
            {
                c.IncludeRegistry<Register>();
                c.For<IMainWindow>().Use(x => Substitute.For<IMainWindow>());
                c.For<IWindowPlacement>().Use(x => Substitute.For<IWindowPlacement>());
            });
            cont.AssertConfigurationIsValid();
        }
    }
}