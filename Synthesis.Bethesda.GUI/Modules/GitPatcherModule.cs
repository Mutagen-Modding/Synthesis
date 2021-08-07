using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.Services.Patchers;
using Synthesis.Bethesda.GUI.Services.Patchers.Git;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;

namespace Synthesis.Bethesda.GUI.Modules
{
    public class GitPatcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<PatcherModule>();
            builder.RegisterAssemblyTypes(typeof(IGithubPatcherIdentifier).Assembly)
                .InNamespacesOf(
                    typeof(IGithubPatcherIdentifier),
                    typeof(IPathToProjProvider))
                .SingleInstance()
                .NotInjection()
                .AsImplementedInterfaces();
            builder.RegisterAssemblyTypes(typeof(GitPatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(GitPatcherVm),
                    typeof(IPrepareRunnableState),
                    typeof(ISolutionFilePathFollower))
                .SingleInstance()
                .NotInjection()
                .AsImplementedInterfaces()
                .AsSelf();
            
            builder.RegisterAssemblyTypes(typeof(GitPatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(GitPatcherInitVm))
                .NotInjection()
                .AsImplementedInterfaces()
                .AsSelf();
            
            builder.RegisterType<PatcherLogDecorator>()
                .AsImplementedInterfaces()
                .SingleInstance();
            
            base.Load(builder);
        }
    }
}