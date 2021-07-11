using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.GUI.Profiles.Plugins;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.DI
{
    public class ProfileModule : Module
    {
        public const string ScopeNickname = "Profile";
        
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProfileVm>().AsSelf()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<GitPatcherInitVm>().AsSelf()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<SolutionPatcherInitVm>().AsSelf()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<CliPatcherInitVm>().AsSelf()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<ExistingSolutionInitVm>().AsSelf();
            builder.RegisterType<ExistingProjectInitVm>().AsSelf();
            builder.RegisterType<NewSolutionInitVm>().AsSelf();
            builder.RegisterType<GitPatcherVm>().AsSelf();
            builder.RegisterType<SolutionPatcherVm>()
                .AsSelf()
                .AsImplementedInterfaces();
            builder.RegisterType<CliPatcherVm>().AsSelf();
            builder.RegisterType<PatchersRunVm>().AsSelf();
            
            // Test
            builder.RegisterType<PatcherSettingsVm>().AsSelf();

            builder.RegisterType<ProfilePatchersList>()
                .As<IRemovePatcherFromProfile>()
                .As<IProfilePatchersList>()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<ProfileLoadOrder>()
                .As<IProfileLoadOrder>()
                .As<ILoadOrderListingsProvider>()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<ProfileDirectories>()
                .As<IProfileDirectories>()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<ProfileDataFolder>()
                .As<IProfileDataFolder>()
                .As<IDataDirectoryProvider>()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<ProfileVersioning>()
                .As<IProfileVersioning>()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            builder.RegisterType<ProfileSimpleLinkCache>()
                .As<IProfileSimpleLinkCache>()
                .InstancePerMatchingLifetimeScope(ScopeNickname);

            builder.RegisterType<ProfileDisplayControllerVm>().As<IProfileDisplayControllerVm>()
                .InstancePerMatchingLifetimeScope(ScopeNickname);
            
            builder.RegisterAssemblyTypes(typeof(PatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(PatcherVm))
                .AsMatchingInterface();
            
            builder.RegisterAssemblyTypes(typeof(PatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(ISolutionMetaFileSync))
                .InstancePerMatchingLifetimeScope(ScopeNickname)
                .AsMatchingInterface();
            
            // Execution lib
            builder.RegisterAssemblyTypes(typeof(IRunner).Assembly)
                .InNamespacesOf(
                    typeof(IRunner))
                .InstancePerMatchingLifetimeScope(ScopeNickname)
                .AsMatchingInterface();
        }
    }
}