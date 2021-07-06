using System;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Services.Profile
{
    public interface IPatcherFactory
    {
        TPatcher Get<TPatcher>()
            where TPatcher : PatcherVM;

        PatcherVM Get(PatcherSettings settings);
    }

    public class PatcherFactory : IPatcherFactory
    {
        private readonly IProfileIdentifier _Ident;

        public PatcherFactory(
            IProfileIdentifier ident)
        {
            _Ident = ident;
        }
        
        public TPatcher Get<TPatcher>()
            where TPatcher : PatcherVM
        {
            return _Ident.Container
                .GetInstance<TPatcher>();
        }

        public PatcherVM Get(PatcherSettings settings)
        {
            return settings switch
            {
                GithubPatcherSettings git => _Ident.Container
                    .With(git)
                    .GetInstance<GitPatcherVM>(),
                SolutionPatcherSettings soln => _Ident.Container
                    .With(soln)
                    .GetInstance<SolutionPatcherVM>(),
                CliPatcherSettings cli => _Ident.Container
                    .With(cli)
                    .GetInstance<CliPatcherVM>(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}