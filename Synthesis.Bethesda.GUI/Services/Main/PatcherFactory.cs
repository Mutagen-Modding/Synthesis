using System;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.Services.Main
{
    public interface IPatcherFactory
    {
        TPatcher Get<TPatcher>()
            where TPatcher : PatcherVM;

        PatcherVM Get(PatcherSettings settings);
    }

    public class PatcherFactory : IPatcherFactory
    {
        private readonly IProfileMetadataRegistry _MetadataRegistry;

        public PatcherFactory(IProfileMetadataRegistry metadataRegistry)
        {
            _MetadataRegistry = metadataRegistry;
        }
        
        public TPatcher Get<TPatcher>()
            where TPatcher : PatcherVM
        {
            return _MetadataRegistry.Configure()
                .GetInstance<TPatcher>();
        }

        public PatcherVM Get(PatcherSettings settings)
        {
            return settings switch
            {
                GithubPatcherSettings git => _MetadataRegistry.Configure()
                    .With(git)
                    .GetInstance<GitPatcherVM>(),
                SolutionPatcherSettings soln => _MetadataRegistry.Configure()
                    .With(soln)
                    .GetInstance<SolutionPatcherVM>(),
                CliPatcherSettings cli => _MetadataRegistry.Configure()
                    .With(cli)
                    .GetInstance<CliPatcherVM>(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}