using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class LoadOrderForRunProviderTests
    {
        [Theory, SynthAutoData]
        public void ReturnsListFromProvider(
            IEnumerable<IModListingGetter> listings,
            ModPath outputPath,
            LoadOrderForRunProvider sut)
        {
            sut.LoadOrderListingsProvider.Get().Returns(listings);
            sut.Get(outputPath)
                .Should().Equal(listings);
        }
        
        [Theory, SynthAutoData]
        public void TrimsPastOutputPath(
            IEnumerable<IModListingGetter> listingsFirst,
            IEnumerable<IModListingGetter> listingsSecond,
            ModPath outputPath,
            LoadOrderForRunProvider sut)
        {
            var modListingGetter = new ModListing(outputPath, true);
            sut.LoadOrderListingsProvider.Get().Returns(
                listingsFirst
                    .And(modListingGetter)
                    .Concat(listingsSecond));
            sut.Get(outputPath)
                .Should().Equal(listingsFirst);
        }
    }
}