using System.Reactive;
using System.Windows.Input;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.GUI;
using Synthesis.Bethesda.GUI.ViewModels.Top;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.ViewModel
{
    public class ConfirmationPanelControllerTests
    {
        [Theory, SynthAutoData]
        public void Discards(
            IConfirmationActionVm confirmVm,
            ConfirmationPanelControllerVm sut)
        {
            sut.TargetConfirmation = confirmVm;
            ((ICommand) sut.DiscardActionCommand).CanExecute(Unit.Default)
                .Should().BeTrue();
            ((ICommand) sut.DiscardActionCommand).Execute(Unit.Default);
            sut.TargetConfirmation.Should().BeNull();
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public void Confirms(
            IConfirmationActionVm confirmVm,
            ConfirmationPanelControllerVm sut)
        {
            sut.TargetConfirmation = confirmVm;
            ((ICommand) sut.ConfirmActionCommand).CanExecute(Unit.Default)
                .Should().BeTrue();
            ((ICommand) sut.ConfirmActionCommand).Execute(Unit.Default);
            sut.TargetConfirmation.Should().BeNull();
            confirmVm.ToDo!.Received().Invoke();
        }
    }
}