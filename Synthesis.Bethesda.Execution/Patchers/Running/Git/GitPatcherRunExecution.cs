using System.Reactive.Disposables;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public class GitPatcherRunExecution : IGitPatcherRunExecution
{
    private readonly CompositeDisposable _disposable = new();

    public Guid Key { get; }
    public int Index { get; }
    public IPatcherNameProvider NameProvider { get; }
    public IGitPatcherRunner GitPatcherRunner { get; }
    public IBuildMetaFilePathProvider BuildMetaFilePathProvider { get; }
    public IBuildMetaFileReader BuildMetaFileReader { get; }
    public IPrintShaIfApplicable PrintShaIfApplicable { get; }
    public string Name => NameProvider.Name;

    public GitPatcherRunExecution(
        IPatcherNameProvider nameProvider,
        IGitPatcherRunner gitPatcherRunner,
        IBuildMetaFilePathProvider buildMetaFilePathProvider,
        IBuildMetaFileReader buildMetaFileReader,
        IPatcherIdProvider idProvider,
        IIndexDisseminator indexDisseminator,
        IPrintShaIfApplicable printShaIfApplicable)
    {
        Key = idProvider.InternalId;
        NameProvider = nameProvider;
        GitPatcherRunner = gitPatcherRunner;
        BuildMetaFilePathProvider = buildMetaFilePathProvider;
        BuildMetaFileReader = buildMetaFileReader;
        PrintShaIfApplicable = printShaIfApplicable;
        Index = indexDisseminator.GetNext();
    }

    public async Task Run(RunSynthesisPatcher settings, PatcherRunCapture capture, CancellationToken cancel)
    {
        PrintShaIfApplicable.Print();

        // Read the meta file to get the ExecutablePath
        var meta = BuildMetaFileReader.Read(BuildMetaFilePathProvider.Path);

        if (meta == null)
        {
            throw new InvalidOperationException($"Failed to read git patcher compilation meta from: {BuildMetaFilePathProvider.Path}");
        }

        await GitPatcherRunner.Run(settings, meta, capture, cancel).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }

    public void Add(IDisposable disposable)
    {
        _disposable.Add(disposable);
    }

    public override string ToString() => Name;
}