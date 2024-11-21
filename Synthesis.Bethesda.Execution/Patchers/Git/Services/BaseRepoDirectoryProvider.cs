using Noggog;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public interface IBaseRepoDirectoryProvider
{
    DirectoryPath Path { get; }
}

public class BaseRepoDirectoryProvider : IBaseRepoDirectoryProvider
{
    private readonly IProfileDirectories _dirs;
    private readonly IGithubPatcherIdentifier _ident;

    public DirectoryPath Path => System.IO.Path.Combine(_dirs.ProfileDirectory, "Git", _ident.Id);
        
    public BaseRepoDirectoryProvider(
        IProfileDirectories dirs,
        IGithubPatcherIdentifier ident)
    {
        _dirs = dirs;
        _ident = ident;
    }
}