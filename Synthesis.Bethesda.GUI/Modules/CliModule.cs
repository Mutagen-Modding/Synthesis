using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.GUI.Services.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;

namespace Synthesis.Bethesda.GUI.Modules
{
    public class CliModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<PatcherModule>();
            
            builder.RegisterAssemblyTypes(typeof(ICliNameConverter).Assembly)
                .InNamespacesOf(
                    typeof(ICliNameConverter))
                .NotInjection()
                .AsImplementedInterfaces();
            
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