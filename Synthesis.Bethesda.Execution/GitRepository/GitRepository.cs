using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface IGitRepository : IDisposable
    {
        IEnumerable<IBranch> Branches { get; }
        IEnumerable<ITag> Tags { get; }
        string CurrentSha { get; }
        IBranch? MainBranch { get; }
        string WorkingDirectory { get; }
        string? MainRemoteUrl { get; }
        void Fetch();
        void ResetHard();
        void ResetHard(ICommit commit);
        ICommit? TryGetCommit(string sha, out bool validSha);
        IBranch TryCreateBranch(string branchName);
        bool TryGetBranch(string branchName, [MaybeNullWhen(false)] out IBranch branch);
        bool TryGetTagSha(string tagName, [MaybeNullWhen(false)] out string sha);
        void Checkout(IBranch branch);
        void Pull();
        void Stage(string path);
        void Commit(string message, Signature signature);
    }

    public class GitRepository : IGitRepository
    {
        private static Signature PlaceholderSignature = new("please", "whymustidothis@gmail.com", DateTimeOffset.Now);
        private readonly Repository _Repository;

        public IEnumerable<IBranch> Branches => _Repository.Branches.Select(x => new BranchWrapper(x));
        public IEnumerable<ITag> Tags => _Repository.Tags.Select(x => new TagWrapper(x));
        public string CurrentSha => _Repository.Head.Tip.Sha;

        public IBranch? MainBranch
        {
            get
            {
                var ret = _Repository.Branches.FirstOrDefault(b => b.IsCurrentRepositoryHead);
                if (ret == null) return null;
                return new BranchWrapper(ret);
            }
        }
        
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

        public void ResetHard(ICommit commit)
        {
            _Repository.Reset(ResetMode.Hard, commit.GetUnderlying(), new CheckoutOptions());
        }

        public ICommit? TryGetCommit(string sha, out bool validSha)
        {
            validSha = ObjectId.TryParse(sha, out var objId);
            if (!validSha) return null;
            var ret = _Repository.Lookup(objId, ObjectType.Commit) as Commit;
            if (ret == null) return null;
            return new CommitWrapper(ret);
        }

        public IBranch TryCreateBranch(string branchName)
        {
            return new BranchWrapper(
                _Repository.Branches[branchName] ?? _Repository.CreateBranch(branchName));
        }

        public bool TryGetBranch(string branchName, [MaybeNullWhen(false)] out IBranch branch)
        {
            var branchDirect = _Repository.Branches[branchName];
            if (branchDirect != null)
            {
                branch = new BranchWrapper(branchDirect);
                return true;
            }

            branch = default;
            return false;
        }

        public bool TryGetTagSha(string tagName, [MaybeNullWhen(false)] out string sha)
        {
            sha = _Repository.Tags[tagName]?.Target.Sha;
            return !sha.IsNullOrWhitespace();
        }

        public void Checkout(IBranch branch)
        {
            Commands.Checkout(_Repository, branch.GetUnderlying());
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