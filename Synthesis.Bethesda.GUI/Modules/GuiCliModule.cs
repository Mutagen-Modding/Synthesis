using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.GUI.Services.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;

namespace Synthesis.Bethesda.GUI.Modules
{
    public class GuiCliModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<PatcherModule>();
            
            builder.RegisterAssemblyTypes(typeof(CliPatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(IPathToExecutableInputVm))
                .SingleInstance()
                .NotInjection()
                .AsImplementedInterfaces()
                .AsSelf();
        }
    }
}