using System.IO.Abstractions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.Projects;
using Noggog.IO;

namespace Synthesis.Bethesda.Execution.Versioning.Query
{
    public interface IPrepLatestVersionProject
    {
        void Prep();
    }

    public class PrepLatestVersionProject : IPrepLatestVersionProject
    {
        public IFileSystem FileSystem { get; }
        public ICreateSolutionFile CreateSolutionFile { get; }
        public ICreateProject CreateProject { get; }
        public IDeleteEntireDirectory DeleteEntireDirectory { get; }
        public IAddProjectToSolution AddProjectToSolution { get; }
        public IQueryVersionProjectPathing Pathing { get; }

        public PrepLatestVersionProject(
            IFileSystem fileSystem,
            ICreateSolutionFile createSolutionFile,
            ICreateProject createProject,
            IDeleteEntireDirectory deleteEntireDirectory,
            IAddProjectToSolution addProjectToSolution,
            IQueryVersionProjectPathing pathing)
        {
            FileSystem = fileSystem;
            CreateSolutionFile = createSolutionFile;
            CreateProject = createProject;
            DeleteEntireDirectory = deleteEntireDirectory;
            AddProjectToSolution = addProjectToSolution;
            Pathing = pathing;
        }
        
        public void Prep()
        {
            DeleteEntireDirectory.DeleteEntireFolder(Pathing.BaseFolder);
            FileSystem.Directory.CreateDirectory(Pathing.BaseFolder);
            CreateSolutionFile.Create(Pathing.SolutionFile);
            CreateProject.Create(GameCategory.Skyrim, Pathing.ProjectFile, insertOldVersion: true);
            AddProjectToSolution.Add(Pathing.SolutionFile, Pathing.ProjectFile);
        }
    }
}