using System;
using Autofac;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers
{
    public class ScopedPatcherFactory
    {
        private readonly Func<GitPatcherVm> _gitPatcherActivator;
        private readonly Func<SolutionPatcherVm> _slnPatcherActivator;
        private readonly Func<CliPatcherVm> _cliPatcherActivator;
        private readonly GitPatcherVm.Factory _gitPatcherFactory;
        private readonly SolutionPatcherVm.Factory _slnPatcherFactory;
        private readonly CliPatcherVm.Factory _cliPatcherFactory;

        public ScopedPatcherFactory(
            Func<GitPatcherVm> gitPatcherActivator,
            Func<SolutionPatcherVm> slnPatcherActivator,
            Func<CliPatcherVm> cliPatcherActivator,
            GitPatcherVm.Factory gitPatcherFactory,
            SolutionPatcherVm.Factory slnPatcherFactory,
            CliPatcherVm.Factory cliPatcherFactory)
        {
            _gitPatcherActivator = gitPatcherActivator;
            _slnPatcherActivator = slnPatcherActivator;
            _cliPatcherActivator = cliPatcherActivator;
            _gitPatcherFactory = gitPatcherFactory;
            _slnPatcherFactory = slnPatcherFactory;
            _cliPatcherFactory = cliPatcherFactory;
        }
        
        public PatcherVm Get(PatcherSettings settings)
        {
            return settings switch
            {
                GithubPatcherSettings git => _gitPatcherFactory(git),
                SolutionPatcherSettings soln => _slnPatcherFactory(soln),
                CliPatcherSettings cli => _cliPatcherFactory(cli),
                _ => throw new NotImplementedException(),
            };
        }

        public GitPatcherVm GetGitPatcher() => _gitPatcherActivator();

        public SolutionPatcherVm GetSolutionPatcher() => _slnPatcherActivator();

        public CliPatcherVm GetCliPatcher() => _cliPatcherActivator();
    }
}