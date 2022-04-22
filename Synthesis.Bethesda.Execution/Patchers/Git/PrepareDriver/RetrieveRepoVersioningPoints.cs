using Noggog;
using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareDriver;

public interface IRetrieveRepoVersioningPoints
{
    void Retrieve(
        IGitRepository repo,
        out List<DriverTag> tags, 
        out Dictionary<string, string> branchShas);
}

public class RetrieveRepoVersioningPoints : IRetrieveRepoVersioningPoints
{
    public void Retrieve(
        IGitRepository repo,
        out List<DriverTag> tags, 
        out Dictionary<string, string> branchShas)
    {
        tags = repo.Tags.Select(tag => (tag.FriendlyName, tag.Sha))
            .WithIndex()
            .Select(x => new DriverTag(x.Index, x.Item.FriendlyName, x.Item.Sha))
            .ToList();
        branchShas = repo.Branches
            .ToDictionary(x => x.FriendlyName, x => x.Tip.Sha, StringComparer.OrdinalIgnoreCase);
    }
}