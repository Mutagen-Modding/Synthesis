using System;
using System.IO;
using System.Runtime.CompilerServices;
using LibGit2Sharp;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Noggog.Utility;

namespace Synthesis.Bethesda.UnitTests
{
    public class RepoTestUtility
    {
        public string DefaultBranch => "master";
        public string AFile => "Somefile.txt";
        public string SlnPath => "Solution.sln";
        public string ProjPath => "MyProj/MyProj.csproj";
        public Signature Signature => new("noggog", "someEmail@gmail.com", DateTimeOffset.Now);
        
        public TempFolder GetRepository(
            string folderName,
            out DirectoryPath remote, 
            out DirectoryPath local,
            bool createPatcherFiles = true,
            [CallerMemberName] string? testName = null)
        {
            var folder = Utility.GetTempFolder(folderName, testName: testName);

            local = Path.Combine(folder.Dir.Path, "Local");
            Repository.Init(local);
            remote = Path.Combine(folder.Dir.Path, "Remote");
            Repository.Init(remote, isBare: true);

            Directory.CreateDirectory(local);
            using var localRepo = new Repository(local);
            File.AppendAllText(Path.Combine(local, AFile), "Hello there");
            Commands.Stage(localRepo, AFile);
            var sig = Signature;
            localRepo.Commit("Initial commit", sig, sig);

            if (createPatcherFiles)
            {
                var files = SolutionInitialization.CreateSolutionFile(Path.Combine(local, SlnPath))
                    .And(SolutionInitialization.CreateProject(Path.Combine(local, ProjPath), GameCategory.Skyrim));
                SolutionInitialization.AddProjectToSolution(Path.Combine(local, SlnPath), Path.Combine(local, ProjPath));
                foreach (var path in files)
                {
                    Commands.Stage(localRepo, path);
                }
                localRepo.Commit("Added solution", sig, sig);
            }

            var remoteRef = localRepo.Network.Remotes.Add("origin", remote);
            var master = localRepo.Branches[DefaultBranch];
            localRepo.Branches.Update(
                master, 
                b => b.Remote = remoteRef.Name, 
                b => b.UpstreamBranch = master.CanonicalName);
            localRepo.Network.Push(master);

            return folder;
        }
    }
}