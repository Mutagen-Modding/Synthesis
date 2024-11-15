using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class RunLoadOrderPreparerTests
{
    [Theory, SynthAutoData]
    public void PassesOutputPathToLoadOrderForRunProvider(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blacklist,
        GroupRunLoadOrderPreparer sut)
    {
        sut.Write(groupRun, blacklist);
        sut.LoadOrderForRunProvider.Received(1).Get(groupRun.ModKey, blacklist);
    }
        
    [Theory, SynthAutoData]
    public void RemovesImplicitMods(
        IGroupRun groupRun,
        GroupRunLoadOrderPreparer sut)
    {
        sut.Write(groupRun, default!);
        sut.LoadOrderWriter.Received(1)
            .Write(
                Arg.Any<FilePath>(),
                Arg.Any<IEnumerable<ILoadOrderListingGetter>>(),
                removeImplicitMods: true);
    }
        
    [Theory, SynthAutoData]
    public void PassesLoadOrderToWriter(
        IGroupRun groupRun,
        IList<ILoadOrderListingGetter> loadOrder,
        GroupRunLoadOrderPreparer sut)
    {
        sut.LoadOrderForRunProvider.Get(default!, default!)
            .ReturnsForAnyArgs(loadOrder);
        sut.Write(groupRun, default!);
        sut.LoadOrderWriter.Received(1)
            .Write(
                Arg.Any<FilePath>(),
                loadOrder,
                Arg.Any<bool>());
    }
        
    [Theory, SynthAutoData]
    public void PassesLoadOrderPathToWriter(
        IGroupRun groupRun,
        FilePath path,
        GroupRunLoadOrderPreparer sut)
    {
        sut.LoadOrderPathProvider.PathFor(groupRun).Returns(path);
        sut.Write(groupRun, default!);
        sut.LoadOrderWriter.Received(1)
            .Write(
                path,
                Arg.Any<IEnumerable<ILoadOrderListingGetter>>(),
                Arg.Any<bool>());
    }
}