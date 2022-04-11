using System;
using LibGit2Sharp;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface ICommit
{
    string Sha { get; }
    string CommitMessage { get; }
    DateTime CommitDate { get; }
    Commit GetUnderlying();
}

public class CommitWrapper : ICommit
{
    private readonly Commit _commit;
    public string Sha => _commit.Sha;
    public string CommitMessage => _commit.Message;
    public DateTime CommitDate => _commit.Author.When.LocalDateTime;

    public CommitWrapper(Commit commit)
    {
        _commit = commit;
    }

    public Commit GetUnderlying() => _commit;
}