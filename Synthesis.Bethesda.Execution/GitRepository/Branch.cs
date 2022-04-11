using System.Diagnostics.CodeAnalysis;
using LibGit2Sharp;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface IBranch
{
    string RemoteName { get; }
    string FriendlyName { get; }
    ICommit Tip { get; }
    Branch GetUnderlying();
}

[ExcludeFromCodeCoverage]
public class BranchWrapper : IBranch
{
    private readonly Branch _branch;
    public string RemoteName => _branch.RemoteName;
    public string FriendlyName => _branch.FriendlyName;
    public ICommit Tip => new CommitWrapper(_branch.Tip);

    public BranchWrapper(Branch branch)
    {
        _branch = branch;
    }

    public Branch GetUnderlying() => _branch;

    public override string ToString()
    {
        return FriendlyName;
    }

    protected bool Equals(BranchWrapper other)
    {
        return string.Equals(_branch.FriendlyName, other._branch.FriendlyName);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((BranchWrapper)obj);
    }

    public override int GetHashCode()
    {
        return _branch.GetHashCode();
    }
}