using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class RunLoadOrderPreparerTests
    {
        [Theory, SynthAutoData]
        public void PassesOutputPathToLoadOrderForRunProvider(
            ModPath outputPath,
            IReadOnlySet<ModKey> blacklist,
            RunLoadOrderPreparer sut)
        {
            sut.Write(outputPath, blacklist);
            sut.LoadOrderForRunProvider.Received(1).Get(outputPath, blacklist);
        }
        
        [Theory, SynthAutoData]
        public void RemovesImplicitMods(
            ModPath outputPath,
            RunLoadOrderPreparer sut)
        {
            sut.Write(outputPath, default!);
            sut.LoadOrderWriter.Received(1)
                .Write(
                    Arg.Any<FilePath>(),
                    Arg.Any<IEnumerable<IModListingGetter>>(),
                    removeImplicitMods: true);
        }
        
        [Theory, SynthAutoData]
        public void PassesLoadOrderToWriter(
            ModPath outputPath,
            IList<IModListingGetter> loadOrder,
            RunLoadOrderPreparer sut)
        {
            sut.LoadOrderForRunProvider.Get(default!, default!)
                .ReturnsForAnyArgs(loadOrder);
            sut.Write(outputPath, default!);
            sut.LoadOrderWriter.Received(1)
                .Write(
                    Arg.Any<FilePath>(),
                    loadOrder,
                    Arg.Any<bool>());
        }
        
        [Theory, SynthAutoData]
        public void PassesLoadOrderPathToWriter(
            ModPath outputPath,
            FilePath path,
            RunLoadOrderPreparer sut)
        {
            sut.LoadOrderPathProvider.Path.Returns(path);
            sut.Write(outputPath, default!);
            sut.LoadOrderWriter.Received(1)
                .Write(
                    path,
                    Arg.Any<IEnumerable<IModListingGetter>>(),
                    Arg.Any<bool>());
        }
    }
}