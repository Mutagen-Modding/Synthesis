using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;

public interface ICheckoutRunnerBranch
{
    void Checkout(IGitRepository repo);
}

public class CheckoutRunnerBranch : ICheckoutRunnerBranch
{
    public const string RunnerBranch = "SynthesisRunner";
        
    public void Checkout(IGitRepository repo)
    {
        var runnerBranch = repo.TryCreateBranch(RunnerBranch);
        if (!repo.CurrentBranch.Equals(runnerBranch))
        {
            repo.ResetHard();
            repo.Checkout(runnerBranch);
        }
    }
}