using GitHubDependents;
using LibGit2Sharp;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.Internal;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.ImpactTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CancellationTokenSource cancel = new();
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    if (args.Length == 2)
                    {
                        await DoWork(args[0], args[1], cancel.Token);
                    }
                    else
                    {
                        await DoWork(null, null, cancel.Token);
                    }
                    System.Console.WriteLine("Press enter to exit.");
                    System.Console.ReadLine();
                }),
                Task.Run(() =>
                {
                    System.Console.ReadLine();
                    cancel.Cancel();
                }));
        }

        static async Task DoWork(
            string? mutagenVersion,
            string? synthesisVersion,
            CancellationToken cancel)
        {
            using var temp = TempFolder.Factory();
            var failedDeps = new List<Dependent>();
            var projResults = new List<(Dependent, string, ErrorResponse)>();

            mutagenVersion ??= Versions.MutagenVersion;
            synthesisVersion ??= Versions.SynthesisVersion;

            System.Console.WriteLine($"Mutagen: {mutagenVersion}");
            System.Console.WriteLine($"Synthesis: {synthesisVersion}");

            var deps = await GitHubDependents.GitHubDependents.GetDependents(
                    user: RegistryConstants.GithubUser,
                    repository: RegistryConstants.GithubRepoName,
                    packageID: RegistryConstants.PackageId,
                    pages: byte.MaxValue)
                .ToArrayAsync();

            await Task.WhenAll(deps.GroupBy(x => x.User).Select(group => Task.Run(async () =>
            {
                cancel.ThrowIfCancellationRequested();
                if (group.Key == null) return;

                await Task.WhenAll(group.Select(dependency => Task.Run(async () =>
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
                        var slnPath = GitPatcherRun.GetPathToSolution(repo.Info.WorkingDirectory);
                        if (slnPath == null)
                        {
                            System.Console.Error.WriteLine($"Could not get path to solution {clonePath}");
                            failedDeps.Add(dependency);
                            return;
                        }

                        GitPatcherRun.SwapInDesiredVersionsForSolution(
                            solutionPath: slnPath,
                            drivingProjSubPath: string.Empty,
                            mutagenVersion: mutagenVersion,
                            out var _,
                            synthesisVersion: synthesisVersion,
                            out var _);

                        foreach (var proj in SolutionPatcherRun.AvailableProjectSubpaths(slnPath))
                        {
                            System.Console.WriteLine($"Checking {group.Key}/{dependency.Repository}:{proj}");
                            cancel.ThrowIfCancellationRequested();
                            var path = Path.Combine(repoDir.FullName, proj);
                            var compile = await DotNetCommands.Compile(path, cancel, null);
                            if (compile.Failed)
                            {
                                System.Console.WriteLine("Failed compilation");
                            }
                            projResults.Add((dependency, proj, compile));
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

            var failed = projResults.Where(p => p.Item3.Failed).ToList();
            if (failed.Count > 0)
            {
                System.Console.WriteLine("Failed projects:");
                foreach (var f in failed.OrderBy(f => f.Item1.User)
                    .CreateOrderedEnumerable(d => d.Item1.Repository, null, true)
                    .CreateOrderedEnumerable(d => d.Item2, null, true))
                {
                    System.Console.WriteLine($"{f.Item1}: {f.Item2}");
                    System.Console.WriteLine();
                }
            }
        }
    }
}
