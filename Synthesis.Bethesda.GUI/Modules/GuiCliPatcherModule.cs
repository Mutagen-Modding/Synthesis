using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.Services.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;

namespace Synthesis.Bethesda.GUI.Modules
{
    public class GuiCliPatcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<GuiCliModule>();
            
            builder.RegisterAssemblyTypes(typeof(CliPatcherVm).Assembly)
                .InNamespacesOf(typeof(CliPatcherVm))
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