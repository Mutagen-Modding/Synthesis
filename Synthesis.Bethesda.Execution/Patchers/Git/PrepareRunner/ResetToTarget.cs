using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner
{
    public interface IResetToTarget
    {
        GetResponse<ResetResults> Reset(
            IGitRepository repo,
            GitPatcherVersioning patcherVersioning,
            CancellationToken cancel);
    }

    [ExcludeFromCodeCoverage]
    public record ResetResults(string? Target, string CommitMessage, DateTime CommitDate);

    public class ResetToTarget : IResetToTarget
    {
        private readonly ILogger _logger;
        public ICheckoutRunnerBranch CheckoutRunnerBranch { get; }
        public IGetRepoTarget GetRepoTarget { get; }
        public IRetrieveCommit RetrieveCommit { get; }

        public ResetToTarget(
            ILogger logger,
            ICheckoutRunnerBranch checkoutRunnerBranch,
            IGetRepoTarget getRepoTarget,
            IRetrieveCommit retrieveCommit)
        {
            _logger = logger;
            CheckoutRunnerBranch = checkoutRunnerBranch;
            GetRepoTarget = getRepoTarget;
            RetrieveCommit = retrieveCommit;
        }
        
        public GetResponse<ResetResults> Reset(
            IGitRepository repo,
            GitPatcherVersioning patcherVersioning,
            CancellationToken cancel)
        {
            CheckoutRunnerBranch.Checkout(repo);
            
            var targets = GetRepoTarget.Get(
                repo, 
                patcherVersioning);
            if (targets.Failed) return targets.BubbleFailure<ResetResults>();

            var commit = RetrieveCommit.TryGet(
                repo,
                targets.Value,
                patcherVersioning,
                cancel);
            if (commit.Failed) return commit.BubbleFailure<ResetResults>();

            cancel.ThrowIfCancellationRequested();

            if (repo.CurrentSha != commit.Value.Sha)
            {
                _logger.Information("Checking out {TargetSha}", targets.Value.TargetSha);
                repo.ResetHard(commit.Value);
            }

            return new ResetResults(targets.Value.Target, commit.Value.CommitMessage, commit.Value.CommitDate);
        }
    }
}