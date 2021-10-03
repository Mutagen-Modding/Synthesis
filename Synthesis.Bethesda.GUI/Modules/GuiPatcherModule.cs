using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Modules
{
    public class GuiPatcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<PatcherModule>();
            builder.RegisterAssemblyTypes(typeof(PatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(PatcherVm),
                    typeof(IPatcherInitVm))
                .NotInjection()
                .InstancePerMatchingLifetimeScope(LifetimeScopes.PatcherNickname)
                .AsImplementedInterfaces()
                .AsSelf();
            builder.RegisterAssemblyTypes(typeof(IPatcherNameProvider).Assembly)
                .InNamespacesOf(
                    typeof(IPatcherRun))
                .AsImplementedInterfaces();
        }
    }
}