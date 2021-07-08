using System.Reactive.Linq;
using AutoFixture;
using NSubstitute;
using Serilog;
using System;
using FluentAssertions;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Versioning;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Versioning
{
    public class NewestLibraryVersionsTests : IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public NewestLibraryVersionsTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Fact]
        public void DotNetNotAcceptable()
        {
            var sdk = Substitute.For<IInstalledSdkProvider>();
            sdk.DotNetSdkInstalled.Returns(_ => Observable.Return(new DotNetVersion(string.Empty, Acceptable: false)));
            var newest = new NewestLibraryVersions(
                _Fixture.Inject.Create<ILogger>(),
                _Fixture.Inject.Create<IQueryNewestLibraryVersions>(),
                sdk,
                _Fixture.Inject.Create<IConsiderPrereleasePreference>());
            newest.NewestSynthesisVersion.Subscribe();
            newest.NewestSynthesisVersion.Subscribe(x =>
            {
                x.Should().BeNull();
            });
            newest.NewestMutagenVersion.Subscribe();
            newest.NewestMutagenVersion.Subscribe(x =>
            {
                x.Should().BeNull();
            });
        }

        [Fact]
        public void ConsiderPrereleases()
        {
            var sdk = Substitute.For<IInstalledSdkProvider>();
            sdk.DotNetSdkInstalled.Returns(_ => Observable.Return(new DotNetVersion(string.Empty, Acceptable: true)));
            var consider = Substitute.For<IConsiderPrereleasePreference>();
            consider.ConsiderPrereleases.Returns(_ => Observable.Return(true));
            var queryNewest = Substitute.For<IQueryNewestLibraryVersions>();
            queryNewest.GetLatestVersions(includePrerelease: true, Arg.Any<string>())
                .Returns(_ => ("PrereleaseMutagenVersion", "PrereleaseSynthesisVersion"));
            queryNewest.GetLatestVersions(includePrerelease: false, Arg.Any<string>())
                .Returns(_ => ("MutagenVersion", "SynthesisVersion"));
            var newest = new NewestLibraryVersions(
                _Fixture.Inject.Create<ILogger>(),
                queryNewest,
                sdk,
                consider);
            newest.NewestSynthesisVersion.Subscribe();
            newest.NewestSynthesisVersion.Subscribe(x =>
            {
                x.Should().Be("PrereleaseSynthesisVersion");
            });
            newest.NewestMutagenVersion.Subscribe();
            newest.NewestMutagenVersion.Subscribe(x =>
            {
                x.Should().Be("PrereleaseMutagenVersion");
            });
        }

        [Fact]
        public void DoNotConsiderPrereleases()
        {
            var sdk = Substitute.For<IInstalledSdkProvider>();
            sdk.DotNetSdkInstalled.Returns(_ => Observable.Return(new DotNetVersion(string.Empty, Acceptable: true)));
            var consider = Substitute.For<IConsiderPrereleasePreference>();
            consider.ConsiderPrereleases.Returns(_ => Observable.Return(false));
            var queryNewest = Substitute.For<IQueryNewestLibraryVersions>();
            queryNewest.GetLatestVersions(includePrerelease: true, Arg.Any<string>())
                .Returns(_ => ("PrereleaseMutagenVersion", "PrereleaseSynthesisVersion"));
            queryNewest.GetLatestVersions(includePrerelease: false, Arg.Any<string>())
                .Returns(_ => ("MutagenVersion", "SynthesisVersion"));
            var newest = new NewestLibraryVersions(
                _Fixture.Inject.Create<ILogger>(),
                queryNewest,
                sdk,
                consider);
            newest.NewestSynthesisVersion.Subscribe();
            newest.NewestSynthesisVersion.Subscribe(x =>
            {
                x.Should().Be("SynthesisVersion");
            });
            newest.NewestMutagenVersion.Subscribe();
            newest.NewestMutagenVersion.Subscribe(x =>
            {
                x.Should().Be("MutagenVersion");
            });
        }
    }
}