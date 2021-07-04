using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Masters;
using Mutagen.Bethesda.Plugins.Masters.DI;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Synthesis.States;
using NSubstitute;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Pipeline.States
{
    public class AddImplicitMastersTests : IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public AddImplicitMastersTests(Fixture fixture)
        {
            _Fixture = fixture;
        }

        private const string ModA = "ModA.esp";
        private const string ModB = "ModB.esp";
        private const string ModC = "ModC.esp";
        private const string ModD = "ModD.esp";

        record Listing(string Mod, params string[] Masters);
        
        private IMasterReferenceReaderFactory GetReaderFactory(params Listing[] listings)
        {
            var masterReaderFactory = Substitute.For<IMasterReferenceReaderFactory>();
            foreach (var listing in listings)
            {
                masterReaderFactory.FromPath(listing.Mod)
                    .Returns(_ =>
                    {
                        var reader = Substitute.For<IMasterReferenceReader>();
                        reader.Masters.Returns(_ =>
                        {
                            return new List<IMasterReferenceGetter>(listing.Masters.Select(m =>
                            {
                                return new MasterReference()
                                {
                                    Master = m
                                };
                            }));
                        });
                        return reader;
                    });
            }
            return masterReaderFactory;
        }

        private IFindImplicitlyIncludedMods GetImplicitlyIncluded(params Listing[] listings)
        {
            return new FindImplicitlyIncludedMods(
                _Fixture.Inject.Create<IDataDirectoryProvider>(),
                GetReaderFactory(listings));
        }

        [Fact]
        public void NothingToDo()
        {
            var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
                new Listing(ModA),
                new Listing(ModB, ModA, ModC)));
            var list = new List<IModListingGetter>()
            {
                new ModListing(ModA, true),
                new ModListing(ModC, true),
                new ModListing(ModB, true),
            };
            adder.Add(list);
            list.Should().HaveCount(3);
            list.All(x => x.Enabled).Should().BeTrue();
        }
        
        [Fact]
        public void EnableOne()
        {
            var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
                new Listing(ModA),
                new Listing(ModB, ModA, ModC)));
            var list = new List<IModListingGetter>()
            {
                new ModListing(ModA, true),
                new ModListing(ModC, false),
                new ModListing(ModB, true),
            };
            adder.Add(list);
            list.Should().HaveCount(3);
            list.All(x => x.Enabled).Should().BeTrue();
        }
        
        [Fact]
        public void SkipUnreferenced()
        {
            var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
                new Listing(ModA),
                new Listing(ModB, ModA)));
            var list = new List<IModListingGetter>()
            {
                new ModListing(ModA, true),
                new ModListing(ModC, false),
                new ModListing(ModB, true),
            };
            adder.Add(list);
            list.Should().HaveCount(3);
            list.Select(x => x.Enabled).ToArray().Should().BeEquivalentTo(
                true, false, true);
        }
        
        [Fact]
        public void RecursiveEnable()
        {
            var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
                new Listing(ModA),
                new Listing(ModB, ModA, ModC),
                new Listing(ModC, ModD)));
            var list = new List<IModListingGetter>()
            {
                new ModListing(ModA, true),
                new ModListing(ModD, false),
                new ModListing(ModC, false),
                new ModListing(ModB, true),
            };
            adder.Add(list);
            list.Should().HaveCount(4);
            list.All(x => x.Enabled).Should().BeTrue();
        }
        
        [Fact]
        public void RecursiveEnableBadLo()
        {
            var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
                new Listing(ModA),
                new Listing(ModB, ModA, ModC),
                new Listing(ModC, ModD)));
            var list = new List<IModListingGetter>()
            {
                new ModListing(ModA, true),
                new ModListing(ModC, false),
                new ModListing(ModB, true),
                new ModListing(ModD, false),
            };
            adder.Add(list);
            list.Should().HaveCount(4);
            list.All(x => x.Enabled).Should().BeTrue();
        }
    }
}