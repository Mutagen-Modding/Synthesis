using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRespository
{
    public interface IGitRepository : IDisposable
    {
        IEnumerable<Branch> Branches { get; }
        IEnumerable<Tag> Tags { get; }
        string CurrentSha { get; }
        Branch? MainBranch { get; }
        string WorkingDirectory { get; }
        string? MainRemoteUrl { get; }
        void Fetch();
        void ResetHard();
        void ResetHard(Commit commit);
        Commit? TryGetCommit(string sha, out bool validSha);
        Branch TryCreateBranch(string branchName);
        bool TryGetBranch(string branchName, [MaybeNullWhen(false)] out Branch branch);
        bool TryGetTagSha(string tagName, [MaybeNullWhen(false)] out string sha);
        void Checkout(Branch branch);
        void Pull();
        void Stage(string path);
        void Commit(string message, Signature signature);
    }

    public class GitRepository : IGitRepository
    {
        private static Signature PlaceholderSignature = new("please", "whymustidothis@gmail.com", DateTimeOffset.Now);
        private readonly Repository _Repository;

        public IEnumerable<Branch> Branches => _Repository.Branches;
        public IEnumerable<Tag> Tags => _Repository.Tags;
        public string CurrentSha => _Repository.Head.Tip.Sha;
        public Branch? MainBranch => _Repository.Branches.FirstOrDefault(b => b.IsCurrentRepositoryHead);
        public string WorkingDirectory => _Repository.Info.WorkingDirectory;
        public string? MainRemoteUrl => _Repository.Network.Remotes.FirstOrDefault()?.Url;

        public GitRepository(Repository repository)
        {
            _Repository = repository;
        }

        public void Fetch()
        {
            _Repository.Fetch();
        }

        public void ResetHard()
        {
            _Repository.Reset(ResetMode.Hard);
        }

        public void ResetHard(Commit commit)
        {
            _Repository.Reset(ResetMode.Hard, commit, new CheckoutOptions());
        }

        public Commit? TryGetCommit(string sha, out bool validSha)
        {
            validSha = ObjectId.TryParse(sha, out var objId);
            if (!validSha) return null;
            return _Repository.Lookup(objId, ObjectType.Commit) as Commit;
        }

        public Branch TryCreateBranch(string branchName)
        {
            return _Repository.Branches[branchName] ?? _Repository.CreateBranch(branchName);
        }

        public bool TryGetBranch(string branchName, [MaybeNullWhen(false)] out Branch branch)
        {
            branch = _Repository.Branches[branchName];
            return branch != null;
        }

        public bool TryGetTagSha(string tagName, [MaybeNullWhen(false)] out string sha)
        {
            sha = _Repository.Tags[tagName]?.Target.Sha;
            return !sha.IsNullOrWhitespace();
        }

        public void Checkout(Branch branch)
        {
            Commands.Checkout(_Repository, branch);
        }
        
        public void Pull()
        {
            Commands.Pull(_Repository, PlaceholderSignature, null);
        }

        public void Stage(string path)
        {
            Commands.Stage(_Repository, path);
        }

        public void Commit(string message, Signature signature)
        {
            _Repository.Commit(message, signature, signature);
        }

        public void Dispose()
        {
            _Repository.Dispose();
        }
    }
}