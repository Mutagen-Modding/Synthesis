using System.IO.Abstractions;
using GitHubDependents;
using LibGit2Sharp;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.IO;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.ImpactTester;

public class Tester
{
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentTemporaryDirectoryProvider _temporaryDirectoryProvider;
    private readonly ITempFolderProvider _tempFolderProvider;
    private readonly IPrintErrorMessage _printErrorMessage;
    private readonly IAvailableProjectsRetriever _availableProjectsRetriever;
    private readonly IModifyRunnerProjects _modifyRunnerProjects;
    private readonly IProvideCurrentVersions _provideCurrentVersions;
    private readonly IBuild _build;

    public Tester(
        IFileSystem fileSystem,
        IEnvironmentTemporaryDirectoryProvider temporaryDirectoryProvider,
        ITempFolderProvider tempFolderProvider,
        IPrintErrorMessage printErrorMessage,
        IAvailableProjectsRetriever availableProjectsRetriever,
        IModifyRunnerProjects modifyRunnerProjects,
        IProvideCurrentVersions provideCurrentVersions,
        IBuild build)
    {
        _fileSystem = fileSystem;
        _temporaryDirectoryProvider = temporaryDirectoryProvider;
        _tempFolderProvider = tempFolderProvider;
        _printErrorMessage = printErrorMessage;
        _availableProjectsRetriever = availableProjectsRetriever;
        _modifyRunnerProjects = modifyRunnerProjects;
        _provideCurrentVersions = provideCurrentVersions;
        _build = build;
    }
        
    public async Task DoWork(
        NugetVersionPair versions,
        CancellationToken cancel)
    {
        var baseFolder = Path.Combine(
            _temporaryDirectoryProvider.Path,
            "SynthesisUnitTests",
            "Impact");
        _fileSystem.Directory.DeleteEntireFolder(baseFolder);
            
        using var temp = _tempFolderProvider.Create(baseFolder, deleteAfter: true);
        var failedDeps = new List<Dependent>();
        var projResults = new List<ProjectResult>();

        versions = versions with
        {
            Mutagen = versions.Mutagen ?? _provideCurrentVersions.MutagenVersion,
            Synthesis = versions.Synthesis ?? _provideCurrentVersions.SynthesisVersion,
        };

        System.Console.WriteLine($"Mutagen: {versions.Mutagen}");
        System.Console.WriteLine($"Synthesis: {versions.Synthesis}");

        var deps = await GitHubDependents.GitHubDependents.GetDependents(
                user: RegistryConstants.GithubUser,
                repository: RegistryConstants.GithubRepoName,
                packageID: RegistryConstants.PackageId,
                pages: byte.MaxValue)
            .ToArrayAsync();

        bool doThreading = false;

        await Task.WhenAll(deps.GroupBy(x => x.User).Select(group => TaskExt.Run(doThreading, async() =>
        {
            cancel.ThrowIfCancellationRequested();
            if (group.Key == null) return;

            await Task.WhenAll(group.Select(dependency => TaskExt.Run(doThreading, async () =>
            {
                cancel.ThrowIfCancellationRequested();
                try
                {
                    if (dependency.User.IsNullOrWhitespace() || dependency.Repository.IsNullOrWhitespace()) return;
                    var repoDir = Directory.CreateDirectory(Path.Combine(temp.Dir.Path, group.Key, dependency.Repository));
                    var clonePath = $"https://github.com/{dependency.User}/{dependency.Repository}";
                    try
                    {
                        Repository.Clone(clonePath, repoDir.FullName);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine($"Failed to clone {clonePath}");
                        System.Console.Error.WriteLine(ex);
                        failedDeps.Add(dependency);
                        return;
                    }

                    cancel.ThrowIfCancellationRequested();
                    using var repo = new Repository(repoDir.FullName);
                    var slnPath = new SolutionFileLocator(
                            IFileSystemExt.DefaultFilesystem)
                        .GetPath(repo.Info.WorkingDirectory);
                    if (slnPath == null)
                    {
                        System.Console.Error.WriteLine($"Could not get path to solution {clonePath}");
                        failedDeps.Add(dependency);
                        return;
                    }

                    _modifyRunnerProjects.Modify(
                        solutionPath: slnPath.Value,
                        drivingProjSubPath: string.Empty,
                        versions: versions,
                        out var _);

                    foreach (var proj in _availableProjectsRetriever.Get(slnPath.Value))
                    {
                        cancel.ThrowIfCancellationRequested();
                        var path = Path.Combine(repoDir.FullName, proj);
                        if (!ApplicabilityTests.IsMutagenPatcherProject(path))
                        {
                            System.Console.WriteLine($"Skipping {group.Key}/{dependency.Repository}:{proj}");
                            continue;
                        }
                        System.Console.WriteLine($"Checking {group.Key}/{dependency.Repository}:{proj}");
                        var compile = await _build.Compile(path, cancel);
                        if (compile.Failed)
                        {
                            System.Console.WriteLine($"Failed compilation {group.Key}/{dependency.Repository}:{proj}");
                        }
                        projResults.Add(new ProjectResult(
                            dependency,
                            $"{Path.GetDirectoryName(slnPath)}\\{group.Key}\\",
                            proj, 
                            compile));
                    }
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine($"Failed to check {dependency}");
                    System.Console.Error.WriteLine(ex);
                    return;
                }
            })));
        })));

        cancel.ThrowIfCancellationRequested();
        System.Console.WriteLine();
        System.Console.WriteLine();
        System.Console.WriteLine();
        System.Console.WriteLine("-------------------------------");
        System.Console.WriteLine();
        System.Console.WriteLine();
        System.Console.WriteLine();

        using (var file = new StreamWriter(File.OpenWrite("Failed Repos.txt")))
        {
            if (failedDeps.Count > 0)
            {
                System.Console.WriteLine("Failed repos:");
                foreach (var f in failedDeps
                             .OrderBy(d => d.User)
                             .CreateOrderedEnumerable(d => d.Repository, null, true))
                {
                    System.Console.WriteLine($"   {f}");
                    file.WriteLine(f.Repository);
                }
            }
        }
        using (var failedProjFile = new StreamWriter(File.OpenWrite("Failed Projects.txt")))
        {
            using (var failedProjCsvFile = new StreamWriter(File.OpenWrite("Failed Projects.csv")))
            {
                failedProjCsvFile.WriteLine($"Succeeded,Repository,Project");
                System.Console.WriteLine("Failed projects:");
                foreach (var f in projResults.OrderBy(f => f.Dependent.User)
                             .CreateOrderedEnumerable(d => d.Dependent.Repository, null, true)
                             .CreateOrderedEnumerable(d => d.ProjSubPath, null, true))
                {
                    if (f.Compile.Failed)
                    {
                        failedProjFile.WriteLine($"  {f.Dependent}: {f.ProjSubPath}");
                        System.Console.WriteLine($"  {f.Dependent}: {f.ProjSubPath}");
                        _printErrorMessage.Print(f.Compile.Reason, f.SolutionFolderPath, (s, _) =>
                        {
                            failedProjFile.WriteLine(s.ToString());
                            Console.WriteLine(s.ToString());
                        });
                        System.Console.WriteLine();
                    }
                    failedProjCsvFile.WriteLine($"{!f.Compile.Failed},{f.Dependent.Repository},{f.ProjSubPath},{f.Compile.Exception}");
                }
            }
        }

        System.Console.WriteLine("Press enter to exit.");
        System.Console.ReadLine();
    }
}