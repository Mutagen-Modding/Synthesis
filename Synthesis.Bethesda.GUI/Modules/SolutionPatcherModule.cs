using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Modules
{
    public class SolutionPatcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<PatcherModule>();
            builder.RegisterAssemblyTypes(typeof(IPathToProjProvider).Assembly)
                .InNamespacesOf(
                    typeof(IPathToProjProvider))
                .SingleInstance()
                .NotInjection()
                .AsImplementedInterfaces();
            builder.RegisterAssemblyTypes(typeof(SolutionPatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(SolutionPatcherVm),
                    typeof(ToSolutionRunner))
                .SingleInstance()
                .NotInjection()
                .AsImplementedInterfaces()
                .AsSelf();
            
            base.Load(builder);
        }
    }
}