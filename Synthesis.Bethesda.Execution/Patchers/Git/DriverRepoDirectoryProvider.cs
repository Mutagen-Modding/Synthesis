using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IDriverRepoDirectoryProvider
    {
        DirectoryPath Path { get; }
    }

    public class DriverRepoDirectoryProvider : IDriverRepoDirectoryProvider
    {
        private readonly IBaseRepoDirectoryProvider _baseRepo;

        public DirectoryPath Path => System.IO.Path.Combine(_baseRepo.Path, "Driver");

        public DriverRepoDirectoryProvider(
            IBaseRepoDirectoryProvider baseRepo)
        {
            _baseRepo = baseRepo;
        }
    }
}