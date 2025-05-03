using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using LibGit2Sharp;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.Projects;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.IO;
using Synthesis.Bethesda.UnitTests.Common;

namespace Synthesis.Bethesda.UnitTests;

public class RepoTestUtilityPayload : IDisposable
{
    public string AFile => "Somefile.txt";
    public string SlnPath => "Solution.sln";
    public string ProjPath => "MyProj/MyProj.csproj";
    public Signature Signature => new("noggog", "someEmail@gmail.com", DateTimeOffset.Now);

    public DirectoryPath Remote { get; private set; }
    public DirectoryPath Local { get; private set; }
    public string DefaultBranchName { get; private set; } = string.Empty;
    public DirectoryPath Temp { get; private set; }

    private readonly CompositeDisposable _disp = new();
    
    public static RepoTestUtilityPayload GetRepository(
        string folderName,
        bool createPatcherFiles = true,
        [CallerMemberName] string? testName = null)
    {
        var ret = new RepoTestUtilityPayload();
        var folder = Utility.GetTempFolder(folderName, testName: testName);
        ret._disp.Add(folder);
        folder.Dir.DeleteEntireFolder(deleteFolderItself: false);
        ret.Temp = folder.Dir;
        
        ret.Local = Path.Combine(folder.Dir.Path, "Local");
        Repository.Init(ret.Local);
        ret.Remote = Path.Combine(folder.Dir.Path, "Remote");
        Repository.Init(ret.Remote, isBare: true);

        Directory.CreateDirectory(ret.Local);
        using var localRepo = new Repository(ret.Local);
        File.AppendAllText(Path.Combine(ret.Local, ret.AFile), "Hello there");
        LibGit2Sharp.Commands.Stage(localRepo, ret.AFile);
        var sig = ret.Signature;
        localRepo.Commit("Initial commit", sig, sig);
        var defaultBranch = localRepo.Branches.First();
        ret.DefaultBranchName = defaultBranch.FriendlyName;
        
        if (createPatcherFiles)
        {
            var fs = new FileSystem();
            var files = new CreateSolutionFile(IFileSystemExt.DefaultFilesystem, new ExportStringToFile(fs)).Create(Path.Combine(ret.Local, ret.SlnPath))
                .And(
                    new CreateProject(
                        IFileSystemExt.DefaultFilesystem,
                        new ProvideCurrentVersions(),
                        new ExportStringToFile(fs))
                        .Create(GameCategory.Skyrim, Path.Combine(ret.Local, ret.ProjPath)));
            new AddProjectToSolution(IFileSystemExt.DefaultFilesystem).Add(Path.Combine(ret.Local, ret.SlnPath), Path.Combine(ret.Local, ret.ProjPath));
            foreach (var path in files)
            {
                LibGit2Sharp.Commands.Stage(localRepo, path);
            }
            localRepo.Commit("Added solution", sig, sig);
        }

        var remoteRef = localRepo.Network.Remotes.Add("origin", ret.Remote);
        localRepo.Branches.Update(
            defaultBranch, 
            b => b.Remote = remoteRef.Name, 
            b => b.UpstreamBranch = defaultBranch.CanonicalName);
        localRepo.Network.Push(defaultBranch);

        return ret;
    }
        
    public Commit AddACommit()
    {
        File.AppendAllText(Path.Combine(Local, AFile), "Hello there");
        using var repo = new Repository(Local);
        LibGit2Sharp.Commands.Stage(repo, AFile);
        var sig = Signature;
        var commit = repo.Commit("A commit", sig, sig);
        repo.Network.Push(repo.Head);
        return commit;
    }

    public void Dispose()
    {
        _disp.Dispose();
    }
}