using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.GUI.Services.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;

namespace Synthesis.Bethesda.GUI.Modules;

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

        base.Load(builder);
    }
}