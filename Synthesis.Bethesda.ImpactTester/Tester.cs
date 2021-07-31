using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHubDependents;
using LibGit2Sharp;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.ImpactTester
{
    public class Tester
    {
        private readonly IPrintErrorMessage _printErrorMessage;
        private readonly IAvailableProjectsRetriever _availableProjectsRetriever;
        private readonly IModifyRunnerProjects _modifyRunnerProjects;
        private readonly IBuild _build;

        public Tester(
            IPrintErrorMessage printErrorMessage,
            IAvailableProjectsRetriever availableProjectsRetriever,
            IModifyRunnerProjects modifyRunnerProjects,
            IBuild build)
        {
            _printErrorMessage = printErrorMessage;
            _availableProjectsRetriever = availableProjectsRetriever;
            _modifyRunnerProjects = modifyRunnerProjects;
            _build = build;
        }
        
        public async Task DoWork(
            NugetVersionPair versions,
            CancellationToken cancel)
        {
            using var temp = TempFolder.Factory();
            var failedDeps = new List<Dependent>();
            var projResults = new List<ProjectResult>();

            versions = versions with
            {
                Mutagen = versions.Mutagen ?? Versions.MutagenVersion,
                Synthesis = versions.Synthesis ?? Versions.SynthesisVersion,
            };

            System.Console.WriteLine($"Mutagen: {versions.Mutagen}");
            System.Console.WriteLine($"Synthesis: {versions.Synthesis}");

            var deps = await GitHubDependents.GitHubDependents.GetDependents(
                    user: RegistryConstants.GithubUser,
                    repository: RegistryConstants.GithubRepoName,
                    packageID: RegistryConstants.PackageId,
                    pages: byte.MaxValue)
                .ToArrayAsync();

            bool doThreading = true;

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
                                System.Console.WriteLine("Failed compilation");
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

            if (failedDeps.Count > 0)
            {
                System.Console.WriteLine("Failed repos:");
                foreach (var f in failedDeps
                    .OrderBy(d => d.User)
                    .CreateOrderedEnumerable(d => d.Repository, null, true))
                {
                    System.Console.WriteLine($"   {f}");
                }
            }

            var failed = projResults.Where(p => p.Compile.Failed).ToList();
            if (failed.Count > 0)
            {
                System.Console.WriteLine("Failed projects:");
                foreach (var f in failed.OrderBy(f => f.Dependent.User)
                    .CreateOrderedEnumerable(d => d.Dependent.Repository, null, true)
                    .CreateOrderedEnumerable(d => d.ProjSubPath, null, true))
                {
                    System.Console.WriteLine($"{f.Dependent}: {f.ProjSubPath}");
                    _printErrorMessage.Print(f.Compile.Reason, f.SolutionFolderPath, (s, _) =>
                    {
                        Console.WriteLine(s.ToString());
                    });
                    System.Console.WriteLine();
                }
            }

            System.Console.WriteLine("Press enter to exit.");
            System.Console.ReadLine();
        }
    }
}