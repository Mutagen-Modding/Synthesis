using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Versioning.Query;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Versioning
{
    public class PrepLatestVersionProjectTests
    {
        [Theory, SynthAutoData]
        public async Task DeletesBaseFolder(
            DirectoryPath dir,
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.BaseFolder.Returns(dir);
            await sut.Prep(cancellationToken);
            sut.DeleteEntireDirectory.Received(1).DeleteEntireFolder(dir);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public async Task CreatesBaseFolder(
            DirectoryPath dir,
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.BaseFolder.Returns(dir);
            await sut.Prep(cancellationToken);
            sut.FileSystem.Directory.Received(1).CreateDirectory(dir);
        }
        
        [Theory, SynthAutoData]
        public async Task CreatesSolutionFile(
            FilePath solutionFile,
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.SolutionFile.Returns(solutionFile);
            await sut.Prep(cancellationToken);
            sut.CreateSolutionFile.Received(1).Create(solutionFile);
        }
        
        [Theory, SynthAutoData]
        public async Task PathingPassedToCreateProject(
            FilePath projFile,
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.ProjectFile.Returns(projFile);
            await sut.Prep(cancellationToken);
            sut.CreateProject.Received(1).Create(
                Arg.Any<GameCategory>(),
                projFile,
                Arg.Any<bool>(),
                targetFramework: "net5.0");
        }
        
        [Theory, SynthAutoData]
        public async Task CreateProjectMakesSkyrim(
            FilePath projFile,
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.ProjectFile.Returns(projFile);
            await sut.Prep(cancellationToken);
            sut.CreateProject.Received(1).Create(
                GameCategory.Skyrim,
                Arg.Any<FilePath>(),
                Arg.Any<bool>(),
                targetFramework: "net5.0");
        }
        
        [Theory, SynthAutoData]
        public async Task CreateProjectInsertsOldVersion(
            FilePath projFile,
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.ProjectFile.Returns(projFile);
            await sut.Prep(cancellationToken);
            sut.CreateProject.Received(1).Create(
                Arg.Any<GameCategory>(),
                Arg.Any<FilePath>(),
                insertOldVersion: true,
                targetFramework: "net5.0");
        }
        
        [Theory, SynthAutoData]
        public async Task PassesPathingToAddProject(
            FilePath solutionFile,
            FilePath projFile,
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.SolutionFile.Returns(solutionFile);
            sut.Pathing.ProjectFile.Returns(projFile);
            await sut.Prep(cancellationToken);
            sut.AddProjectToSolution.Received(1).Add(solutionFile, projFile);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public async Task Order(
            CancellationToken cancellationToken,
            PrepLatestVersionProject sut)
        {
            await sut.Prep(cancellationToken); 
            Received.InOrder(() =>
            {
                sut.DeleteEntireDirectory.DeleteEntireFolder(Arg.Any<DirectoryPath>());
                sut.FileSystem.Directory.CreateDirectory(Arg.Any<string>());
                sut.CreateSolutionFile.Create(Arg.Any<FilePath>());
                sut.CreateProject.Create(Arg.Any<GameCategory>(), Arg.Any<FilePath>(), Arg.Any<bool>(), Arg.Any<string>());
                sut.AddProjectToSolution.Add(Arg.Any<FilePath>(), Arg.Any<FilePath>());
            });
        }
    }
}