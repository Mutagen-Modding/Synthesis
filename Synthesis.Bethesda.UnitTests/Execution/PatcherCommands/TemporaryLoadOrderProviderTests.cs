using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Noggog.IO;
using NSubstitute;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.PatcherCommands;

public class TemporaryLoadOrderProviderTests
{
    [Theory, SynthAutoData]
    public void PassesPathToTempFileProvider(
        IEnumerable<IModListingGetter> loadOrder,
        FileName randomFileName,
        TemporaryLoadOrderProvider sut)
    {
        sut.RandomFileNameProvider.Get().Returns(randomFileName);
        sut.Get(loadOrder);
        sut.TempFileProvider.Received(1)
            .Create(System.IO.Path.Combine(
                sut.Paths.WorkingDirectory,
                TemporaryLoadOrderProvider.Folder,
                randomFileName));
    }
        
    [Theory, SynthAutoData]
    public void TempFileIsNotDisposed(
        ITempFile tempFile,
        IEnumerable<IModListingGetter> loadOrder,
        TemporaryLoadOrderProvider sut)
    {
        sut.TempFileProvider.Create(default).ReturnsForAnyArgs(tempFile);
        sut.Get(loadOrder)
            .DidNotReceive().Dispose();
    }
        
    [Theory, SynthAutoData]
    public void TempFilePassedToWriter(
        ITempFile tempFile,
        IEnumerable<IModListingGetter> loadOrder,
        TemporaryLoadOrderProvider sut)
    {
        sut.TempFileProvider.Create(default).ReturnsForAnyArgs(tempFile);
        sut.Get(loadOrder);
        sut.LoadOrderWriter.Received(1).Write(
            tempFile.File.Path, 
            Arg.Any<IEnumerable<IModListingGetter>>(),
            Arg.Any<bool>());
    }
        
    [Theory, SynthAutoData]
    public void WriterRemovesImplicitMods(
        IEnumerable<IModListingGetter> loadOrder,
        TemporaryLoadOrderProvider sut)
    {
        sut.Get(loadOrder);
        sut.LoadOrderWriter.Received(1).Write(
            Arg.Any<FilePath>(),
            Arg.Any<IEnumerable<IModListingGetter>>(),
            removeImplicitMods: true);
    }
        
    [Theory, SynthAutoData]
    public void PassesLoadOrderToWriter(
        IEnumerable<IModListingGetter> loadOrder,
        TemporaryLoadOrderProvider sut)
    {
        sut.Get(loadOrder);
        sut.LoadOrderWriter.Received(1).Write(
            Arg.Any<FilePath>(),
            loadOrder,
            Arg.Any<bool>());
    }
}