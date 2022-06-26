using System.Diagnostics.CodeAnalysis;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface ICheckOriginRepoIsValid
{
    ErrorResponse IsValidRepository(string path);
}

[ExcludeFromCodeCoverage]
public class CheckOriginRepoIsValid : ICheckOriginRepoIsValid
{
    public ErrorResponse IsValidRepository(string path)
    {
        try
        {
            if (Repository.ListRemoteReferences(path).Any()) return ErrorResponse.Success;

            return ErrorResponse.Fail("No remote references found");
        }
        catch (Exception ex)
        {
            return ErrorResponse.Fail(ex);
        }
    }
}