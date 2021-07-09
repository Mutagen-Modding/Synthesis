using Autofac;
using Mutagen.Bethesda.Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog.Autofac;
using Synthesis.Bethesda.GUI.Profiles.Plugins;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.DI
{
    public class ProfileModule : Module
    {
        private readonly IProfileIdentifier _Ident;

        public ProfileModule(IProfileIdentifier ident)
        {
            _Ident = ident;
        }
        
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProfileVm>().AsSelf()
                .SingleInstance();
            builder.RegisterType<GitPatcherInitVm>().AsSelf()
                .SingleInstance();
            builder.RegisterType<SolutionPatcherInitVm>().AsSelf()
                .SingleInstance();
            builder.RegisterType<CliPatcherInitVm>().AsSelf()
                .SingleInstance();
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
            
            builder.RegisterInstance(_Ident)
                .As<IProfileIdentifier>()
                .As<IGameReleaseContext>();

            builder.RegisterType<ProfilePatchersList>()
                .As<IRemovePatcherFromProfile>()
                .As<IProfilePatchersList>()
                .SingleInstance();
            builder.RegisterType<ProfileLoadOrder>()
                .As<IProfileLoadOrder>()
                .As<ILoadOrderListingsProvider>()
                .SingleInstance();
            builder.RegisterType<ProfileDirectories>()
                .As<IProfileDirectories>()
                .SingleInstance();
            builder.RegisterType<ProfileDataFolder>()
                .As<IProfileDataFolder>()
                .As<IDataDirectoryProvider>()
                .SingleInstance();
            builder.RegisterType<ProfileVersioning>()
                .As<IProfileVersioning>()
                .SingleInstance();
            builder.RegisterType<ProfileSimpleLinkCache>()
                .As<IProfileSimpleLinkCache>()
                .SingleInstance();

            builder.RegisterType<ProfileDisplayControllerVm>().As<IProfileDisplayControllerVm>()
                .SingleInstance();
            
            builder.RegisterAssemblyTypes(typeof(PatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(PatcherVm))
                .AsMatchingInterface();
            
            builder.RegisterAssemblyTypes(typeof(PatcherVm).Assembly)
                .InNamespacesOf(
                    typeof(ISolutionMetaFileSync))
                .SingleInstance()
                .AsMatchingInterface();
        }
    }
}