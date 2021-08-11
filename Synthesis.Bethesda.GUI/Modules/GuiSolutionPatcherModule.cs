using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.GUI.Services.Patchers;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Modules
{
    public class GuiSolutionPatcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<GuiPatcherModule>();
            builder.RegisterModule<SolutionPatcherModule>();
            builder.RegisterAssemblyTypes(typeof(SolutionPatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(SolutionPatcherVm),
                    typeof(ISolutionFilePathFollower))
                .SingleInstance()
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