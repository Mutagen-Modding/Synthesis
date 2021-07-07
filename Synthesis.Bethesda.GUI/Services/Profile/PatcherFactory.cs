using System;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Services.Profile
{
    public interface IPatcherFactory
    {
        GitPatcherVM GetGitPatcher();
        SolutionPatcherVM GetSolutionPatcher();
        CliPatcherVM GetCliPatcher();
        
        GitPatcherVM Get(GithubPatcherSettings settings);
        SolutionPatcherVM Get(SolutionPatcherSettings settings);
        CliPatcherVM Get(CliPatcherSettings settings);
    }
}