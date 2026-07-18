using System.IO.Abstractions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order.DI;
using Synthesis.Bethesda.Execution.Groups;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IGroupRunLoadOrderPreparer
{
    void Write(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blackListedMods);
}

public class GroupRunLoadOrderPreparer : IGroupRunLoadOrderPreparer
{
    private readonly IFileSystem _fileSystem;
    private readonly ISplitModsAdjacencyValidator _splitModsValidator;
    public ILoadOrderForRunProvider LoadOrderForRunProvider { get; }
    public ILoadOrderPrinter Printer { get; }
    public IRunLoadOrderPathProvider LoadOrderPathProvider { get; }
    public ILoadOrderWriter LoadOrderWriter { get; }

    public GroupRunLoadOrderPreparer(
        IFileSystem fileSystem,
        ILoadOrderForRunProvider loadOrderForRunProvider,
        ILoadOrderPrinter printer,
        IRunLoadOrderPathProvider runLoadOrderPathProvider,
        ILoadOrderWriter loadOrderWriter,
        ISplitModsAdjacencyValidator splitModsValidator)
    {
        _fileSystem = fileSystem;
        LoadOrderForRunProvider = loadOrderForRunProvider;
        Printer = printer;
        LoadOrderPathProvider = runLoadOrderPathProvider;
        LoadOrderWriter = loadOrderWriter;
        _splitModsValidator = splitModsValidator;
    }

    public void Write(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blackListedMods)
    {
        var loadOrderList = LoadOrderForRunProvider.Get(groupRun.ModKey, blackListedMods);

        _splitModsValidator.Validate(loadOrderList);

        Printer.Print(loadOrderList);

        var loadOrderPath = LoadOrderPathProvider.PathFor(groupRun);
        _fileSystem.Directory.CreateDirectory(loadOrderPath.Directory!);
            
        LoadOrderWriter.Write(
            loadOrderPath,
            loadOrderList,
            removeImplicitMods: true);
    }
}