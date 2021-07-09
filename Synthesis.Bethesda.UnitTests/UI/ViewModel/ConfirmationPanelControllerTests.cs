using System.Reactive;
using System.Windows.Input;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.GUI;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.ViewModel
{
    public class ConfirmationPanelControllerTests : IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public ConfirmationPanelControllerTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Fact]
        public void Discards()
        {
            var confirm = new ConfirmationPanelControllerVm();
            confirm.TargetConfirmation = _Fixture.Inject.Create<IConfirmationActionVm>();
            ((ICommand) confirm.DiscardActionCommand).CanExecute(Unit.Default)
                .Should().BeTrue();
            ((ICommand) confirm.DiscardActionCommand).Execute(Unit.Default);
            confirm.TargetConfirmation.Should().BeNull();
        }
        
        [Fact]
        public void Confirms()
        {
            var confirmAction = Substitute.For<IConfirmationActionVm>();
            var confirm = new ConfirmationPanelControllerVm();
            confirm.TargetConfirmation = confirmAction;
            ((ICommand) confirm.ConfirmActionCommand).CanExecute(Unit.Default)
                .Should().BeTrue();
            ((ICommand) confirm.ConfirmActionCommand).Execute(Unit.Default);
            confirm.TargetConfirmation.Should().BeNull();
            confirmAction.ToDo!.Received().Invoke();
        }
    }
}