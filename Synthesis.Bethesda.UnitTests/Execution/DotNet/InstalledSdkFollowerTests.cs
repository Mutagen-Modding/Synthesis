using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet
{
    public class InstalledSdkFollowerTests
    {
        [Theory, SynthAutoData]
        public void QueriesInitially(
            TestScheduler scheduler,
            InstalledSdkFollower sut)
        {
            scheduler.Start(() => sut.DotNetSdkInstalled, 0, 0, 10);
            sut.Query.ReceivedWithAnyArgs(1).Query(default);
        }
        
        [Theory, SynthAutoData]
        public void RequeriesOnFailure(
            TestScheduler scheduler,
            InstalledSdkFollower sut)
        {
            sut.Query.Query(default).ThrowsForAnyArgs<NotImplementedException>();
            var unsubTime = InstalledSdkFollower.RequeryTime * 10;
            scheduler.Start(() => sut.DotNetSdkInstalled, 0, 0, unsubTime.Ticks + 5);
            sut.Query.ReceivedWithAnyArgs(11).Query(default);
        }
        
        [Theory, SynthAutoData]
        public void StopsQueryingOnSuccess(
            TestScheduler scheduler,
            InstalledSdkFollower sut)
        {
            sut.Query.Query(default).Returns(
                x => new DotNetVersion(string.Empty, Acceptable: false),
                x => new DotNetVersion(string.Empty, Acceptable: false),
                x => new DotNetVersion(string.Empty, Acceptable: true));
            var unsubTime = InstalledSdkFollower.RequeryTime * 10;
            scheduler.Start(() => sut.DotNetSdkInstalled, 0, 0, unsubTime.Ticks + 5);
            sut.Query.ReceivedWithAnyArgs(3).Query(default);
        }
        
        [Theory, SynthAutoData]
        public void OnlyReturnsDistinct(
            TestScheduler scheduler,
            InstalledSdkFollower sut)
        {
            sut.Query.Query(default).Returns(
                x => new DotNetVersion(string.Empty, Acceptable: false),
                x => new DotNetVersion(string.Empty, Acceptable: false),
                x => new DotNetVersion(string.Empty, Acceptable: true));
            var unsubTime = InstalledSdkFollower.RequeryTime * 10;
            var messages = scheduler.Start(() => sut.DotNetSdkInstalled, 0, 0, unsubTime.Ticks + 5);
            messages.Messages.Where(x => x.Value.Kind == NotificationKind.OnNext)
                .Should().HaveCount(2);
        }
        
        [Theory, SynthAutoData]
        public void ReturnsSuccess(
            string version,
            TestScheduler scheduler,
            InstalledSdkFollower sut)
        {
            sut.Query.Query(default).Returns(
                x => new DotNetVersion(string.Empty, Acceptable: false),
                x => new DotNetVersion(version, Acceptable: true));
            var unsubTime = InstalledSdkFollower.RequeryTime * 2;
            var messages = scheduler.Start(() => sut.DotNetSdkInstalled, 0, 0, unsubTime.Ticks);
            var last = messages.Messages.Where(x => x.Value.Kind == NotificationKind.OnNext)
                .Last().Value.Value;
            last.Version.Should().Be(version);
            last.Acceptable.Should().BeTrue();
        }
    }
}