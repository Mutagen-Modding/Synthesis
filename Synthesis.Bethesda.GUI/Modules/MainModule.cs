using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Mutagen.Bethesda.Synthesis.Profiles;
using Mutagen.Bethesda.Synthesis.Projects;
using Mutagen.Bethesda.Synthesis.Versioning;
using Mutagen.Bethesda.Synthesis.WPF;
using Noggog.Autofac;
using Noggog.Autofac.Modules;
using Noggog.DotNetCli.DI;
using Noggog.IO;
using Noggog.Nuget.Services.Singleton;
using Noggog.Processes.DI;
using Noggog.Reactive;
using Noggog.Time;
using Noggog.WPF;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.GUI.Args;
using Synthesis.Bethesda.GUI.Json;
using Synthesis.Bethesda.GUI.Logging;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Services.Profile.Exporter;
using Synthesis.Bethesda.GUI.Services.Profile.Running;
using Synthesis.Bethesda.GUI.Services.Profile.TopLevel;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.GUI.Services.Versioning;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Confirmations;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.ViewModels.Top;
using Log = Synthesis.Bethesda.GUI.Logging.Log;

namespace Synthesis.Bethesda.GUI.Modules;

public class MainModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        TopLevel(builder);

        ProfileLevel(builder);
    }

    private static void TopLevel(ContainerBuilder builder)
    {
        builder.RegisterType<FileSystem>().As<IFileSystem>()
            .SingleInstance();
        builder.RegisterModule<NoggogModule>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();

        // Noggog
        builder.RegisterType<WatchFile>().As<IWatchFile>()
            .SingleInstance();
        builder.RegisterType<WatchDirectory>().As<IWatchDirectory>()
            .SingleInstance();
        builder.RegisterType<SchedulerProvider>().As<ISchedulerProvider>()
            .SingleInstance();
        builder.RegisterAssemblyTypes(typeof(IAnalyzeNugetConfig).Assembly)
            .InNamespacesOf(
                typeof(IAnalyzeNugetConfig))
            .TypicalRegistrations()
            .AsImplementedInterfaces()
            .SingleInstance();

        // Mutagen
        builder.RegisterModule<MutagenModule>();

        // Mutagen.Bethesda.Synthesis
        builder.RegisterAssemblyTypes(typeof(IProvideCurrentVersions).Assembly)
            .InNamespacesOf(
                typeof(IProvideCurrentVersions),
                typeof(CreateProfileId),
                typeof(ICreateProject))
            .AsSelf()
            .AsMatchingInterface()
            .SingleInstance();

        // Mutagen.Bethesda.Synthesis.WPF
        builder.RegisterAssemblyTypes(typeof(IProvideAutogeneratedSettings).Assembly)
            .InNamespaceOf<IProvideAutogeneratedSettings>()
            .AsSelf()
            .AsMatchingInterface();

        builder.RegisterModule<Synthesis.Bethesda.Execution.Modules.MainModule>();
            
        builder.RegisterAssemblyTypes(typeof(IExecuteRunnabilityCheck).Assembly)
            .InNamespacesOf(
                typeof(IExecuteRunnabilityCheck),
                typeof(IBuild))
            .AsMatchingInterface();

        builder.RegisterType<WorkingDirectoryProvider>().AsSelf();

        // Top Level
        builder.RegisterAssemblyTypes(typeof(INavigateTo).Assembly)
            .InNamespacesOf(
                typeof(MainVm),
                typeof(INavigateTo),
                typeof(IStartup),
                typeof(ArgClassProvider),
                typeof(ILogSettings),
                typeof(IGuiSettingsImporter),
                typeof(INewestLibraryVersionsVm),
                typeof(ISynthesisGuiSettings))
            .Except<ArgumentReceiver>()
            .AsImplementedInterfaces()
            .AsSelf()
            .SingleInstance();
    }

    private static void ProfileLevel(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(ProfileVm).Assembly)
            .InNamespacesOf(
                typeof(ProfileVm),
                typeof(ConsiderPrereleasePreference))
            .NotInNamespacesOf(typeof(RunVm), typeof(PatcherInitRenameActionVm))
            .InstancePerMatchingLifetimeScope(LifetimeScopes.ProfileNickname)
            .AsImplementedInterfaces()
            .AsSelf();
            
        builder.RegisterAssemblyTypes(typeof(ProfileVm).Assembly)
            .InNamespacesOf(
                typeof(GroupVm),
                typeof(IProfileExporter))
            .AsImplementedInterfaces()
            .AsSelf();
            
        builder.RegisterAssemblyTypes(typeof(IEnvironmentErrorVm).Assembly)
            .InNamespacesOf(
                typeof(IEnvironmentErrorVm))
            .AsImplementedInterfaces();
            
        builder.RegisterAssemblyTypes(typeof(ProfileVm).Assembly)
            .InNamespacesOf(
                typeof(RunVm),
                typeof(IExecuteGuiRun))
            .AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopes.RunNickname)
            .AsSelf();
            
        builder.RegisterType<RxReporter>()
            .InstancePerMatchingLifetimeScope(LifetimeScopes.ProfileNickname)
            .AsImplementedInterfaces();
            

        builder.RegisterModule<Synthesis.Bethesda.Execution.Modules.ProfileModule>();
    }
}