using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog.Autofac;
using Noggog.Autofac.Modules;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.ImpactTester;

public class MainModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FileSystem>().As<IFileSystem>()
            .SingleInstance();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
        builder.RegisterModule<NoggogModule>();
        
        builder.RegisterType<PipelineSettings>().AsImplementedInterfaces();
        builder.RegisterType<DotNetPathSettingsInjection>().AsImplementedInterfaces();

        builder.RegisterModule<Synthesis.Bethesda.Execution.Modules.MainModule>();
        builder.RegisterModule<Synthesis.Bethesda.Execution.Modules.SolutionPatcherModule>();
        builder.RegisterModule<Synthesis.Bethesda.Execution.Modules.GitPatcherModule>();
            
        builder.RegisterAssemblyTypes(typeof(DotNetVersion).Assembly)
            .InNamespacesOf(
                typeof(DotNetVersion),
                typeof(ISynthesisSubProcessRunner))
            .AsImplementedInterfaces();
            
        builder.RegisterAssemblyTypes(typeof(IProvideCurrentVersions).Assembly)
            .InNamespacesOf(
                typeof(IProvideCurrentVersions))
            .AsImplementedInterfaces();
            
        builder.RegisterType<Tester>().AsSelf();
    }
}