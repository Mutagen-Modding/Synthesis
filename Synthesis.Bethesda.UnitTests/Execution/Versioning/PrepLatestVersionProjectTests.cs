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
        public void DeletesBaseFolder(
            DirectoryPath dir,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.BaseFolder.Returns(dir);
            sut.Prep();
            sut.DeleteEntireDirectory.Received(1).DeleteEntireFolder(dir);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void CreatesBaseFolder(
            DirectoryPath dir,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.BaseFolder.Returns(dir);
            sut.Prep();
            sut.FileSystem.Directory.Received(1).CreateDirectory(dir);
        }
        
        [Theory, SynthAutoData]
        public void CreatesSolutionFile(
            FilePath solutionFile,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.SolutionFile.Returns(solutionFile);
            sut.Prep();
            sut.CreateSolutionFile.Received(1).Create(solutionFile);
        }
        
        [Theory, SynthAutoData]
        public void PathingPassedToCreateProject(
            FilePath projFile,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.ProjectFile.Returns(projFile);
            sut.Prep();
            sut.CreateProject.Received(1).Create(
                Arg.Any<GameCategory>(),
                projFile,
                Arg.Any<bool>());
        }
        
        [Theory, SynthAutoData]
        public void CreateProjectMakesSkyrim(
            FilePath projFile,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.ProjectFile.Returns(projFile);
            sut.Prep();
            sut.CreateProject.Received(1).Create(
                GameCategory.Skyrim,
                Arg.Any<FilePath>(),
                Arg.Any<bool>());
        }
        
        [Theory, SynthAutoData]
        public void CreateProjectInsertsOldVersion(
            FilePath projFile,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.ProjectFile.Returns(projFile);
            sut.Prep();
            sut.CreateProject.Received(1).Create(
                Arg.Any<GameCategory>(),
                Arg.Any<FilePath>(),
                insertOldVersion: true);
        }
        
        [Theory, SynthAutoData]
        public void PassesPathingToAddProject(
            FilePath solutionFile,
            FilePath projFile,
            PrepLatestVersionProject sut)
        {
            sut.Pathing.SolutionFile.Returns(solutionFile);
            sut.Pathing.ProjectFile.Returns(projFile);
            sut.Prep();
            sut.AddProjectToSolution.Received(1).Add(solutionFile, projFile);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void Order(
            PrepLatestVersionProject sut)
        {
            sut.Prep(); 
            Received.InOrder(() =>
            {
                sut.DeleteEntireDirectory.DeleteEntireFolder(Arg.Any<DirectoryPath>());
                sut.FileSystem.Directory.CreateDirectory(Arg.Any<string>());
                sut.CreateSolutionFile.Create(Arg.Any<FilePath>());
                sut.CreateProject.Create(Arg.Any<GameCategory>(), Arg.Any<FilePath>(), Arg.Any<bool>());
                sut.AddProjectToSolution.Add(Arg.Any<FilePath>(), Arg.Any<FilePath>());
            });
        }
    }
}