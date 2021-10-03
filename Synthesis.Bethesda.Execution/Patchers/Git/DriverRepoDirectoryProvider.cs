using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IDriverRepoDirectoryProvider
    {
        DirectoryPath Path { get; }
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    public record DriverRepoDirectoryInjection(DirectoryPath Path) : IDriverRepoDirectoryProvider;
}