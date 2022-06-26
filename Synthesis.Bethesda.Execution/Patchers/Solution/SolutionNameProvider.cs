using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public class SolutionNameProvider : IPatcherNameProvider
{
    private readonly IPatcherNicknameProvider _nicknameProvider;
    private readonly SolutionNameConstructor _nameConstructor;
    private readonly IProjectSubpathProvider _projectSubpathProvider;

    public string Name =>
        _nameConstructor.Construct(_nicknameProvider.Nickname, _projectSubpathProvider.ProjectSubpath);

    public SolutionNameProvider(
        IPatcherNicknameProvider nicknameProvider,
        SolutionNameConstructor nameConstructor,
        IProjectSubpathProvider projectSubpathProvider)
    {
        _nicknameProvider = nicknameProvider;
        _nameConstructor = nameConstructor;
        _projectSubpathProvider = projectSubpathProvider;
    }
}