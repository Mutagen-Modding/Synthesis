using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Masters;
using Mutagen.Bethesda.Plugins.Masters.DI;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Synthesis.States;
using NSubstitute;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Pipeline.States;

public class AddImplicitMastersTests
{
    private const string ModA = "ModA.esp";
    private const string ModB = "ModB.esp";
    private const string ModC = "ModC.esp";
    private const string ModD = "ModD.esp";

    record Listing(string Mod, params string[] Masters);
        
    private IMasterReferenceReaderFactory GetReaderFactory(
        MockFileSystem mockFileSystem,
        IDataDirectoryProvider dataDirectoryProvider,
        params Listing[] listings)
    {
        var masterReaderFactory = Substitute.For<IMasterReferenceReaderFactory>();
        foreach (var listing in listings)
        {
            mockFileSystem.File.WriteAllText(Path.Combine(dataDirectoryProvider.Path, listing.Mod), string.Empty);
            masterReaderFactory.FromPath(Path.Combine(dataDirectoryProvider.Path, listing.Mod))
                .Returns(_ =>
                {
                    var reader = Substitute.For<IReadOnlyMasterReferenceCollection>();
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

    private IFindImplicitlyIncludedMods GetImplicitlyIncluded(
        MockFileSystem mockFileSystem,
        IDataDirectoryProvider dataDirectoryProvider,
        params Listing[] listings)
    {
        return new FindImplicitlyIncludedMods(
            mockFileSystem,
            dataDirectoryProvider,
            GetReaderFactory(mockFileSystem, dataDirectoryProvider, listings));
    }

    [Theory, SynthAutoData]
    public void NothingToDo(
        MockFileSystem mockFileSystem,
        IDataDirectoryProvider dataDirectoryProvider)
    {
        var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
            mockFileSystem,
            dataDirectoryProvider,
            new Listing(ModA),
            new Listing(ModB, ModA, ModC),
            new Listing(ModC)));
        var list = new List<ILoadOrderListingGetter>()
        {
            new LoadOrderListing(ModA, true),
            new LoadOrderListing(ModC, true),
            new LoadOrderListing(ModB, true),
        };
        adder.Add(list);
        list.Should().HaveCount(3);
        list.All(x => x.Enabled).Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public void EnableOne(
        MockFileSystem mockFileSystem,
        IDataDirectoryProvider dataDirectoryProvider)
    {
        var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
            mockFileSystem,
            dataDirectoryProvider,
            new Listing(ModA),
            new Listing(ModB, ModA, ModC),
            new Listing(ModC)));
        var list = new List<ILoadOrderListingGetter>()
        {
            new LoadOrderListing(ModA, true),
            new LoadOrderListing(ModC, false),
            new LoadOrderListing(ModB, true),
        };
        adder.Add(list);
        list.Should().HaveCount(3);
        list.All(x => x.Enabled).Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public void SkipUnreferenced(
        MockFileSystem mockFileSystem,
        IDataDirectoryProvider dataDirectoryProvider)
    {
        var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
            mockFileSystem,
            dataDirectoryProvider,
            new Listing(ModA),
            new Listing(ModB, ModA)));
        var list = new List<ILoadOrderListingGetter>()
        {
            new LoadOrderListing(ModA, true),
            new LoadOrderListing(ModC, false),
            new LoadOrderListing(ModB, true),
        };
        adder.Add(list);
        list.Should().HaveCount(3);
        list.Select(x => x.Enabled).ToArray().Should().Equal(
            true, false, true);
    }
        
    [Theory, SynthAutoData]
    public void RecursiveEnable(
        MockFileSystem mockFileSystem,
        IDataDirectoryProvider dataDirectoryProvider)
    {
        var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
            mockFileSystem,
            dataDirectoryProvider,
            new Listing(ModA),
            new Listing(ModB, ModA, ModC),
            new Listing(ModC, ModD),
            new Listing(ModD)));
        var list = new List<ILoadOrderListingGetter>()
        {
            new LoadOrderListing(ModA, true),
            new LoadOrderListing(ModD, false),
            new LoadOrderListing(ModC, false),
            new LoadOrderListing(ModB, true),
        };
        adder.Add(list);
        list.Should().HaveCount(4);
        list.All(x => x.Enabled).Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public void RecursiveEnableBadLo(
        MockFileSystem mockFileSystem,
        IDataDirectoryProvider dataDirectoryProvider)
    {
        var adder = new EnableImplicitMasters(GetImplicitlyIncluded(
            mockFileSystem,
            dataDirectoryProvider,
            new Listing(ModA),
            new Listing(ModB, ModA, ModC),
            new Listing(ModC, ModD),
            new Listing(ModD)));
        var list = new List<ILoadOrderListingGetter>()
        {
            new LoadOrderListing(ModA, true),
            new LoadOrderListing(ModC, false),
            new LoadOrderListing(ModB, true),
            new LoadOrderListing(ModD, false),
        };
        adder.Add(list);
        list.Should().HaveCount(4);
        list.All(x => x.Enabled).Should().BeTrue();
    }
}