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
            builder.RegisterType<ProfileVM>().AsSelf()
                .SingleInstance();
            builder.RegisterType<GitPatcherInitVM>().AsSelf()
                .SingleInstance();
            builder.RegisterType<SolutionPatcherInitVM>().AsSelf()
                .SingleInstance();
            builder.RegisterType<CliPatcherInitVM>().AsSelf()
                .SingleInstance();
            builder.RegisterType<ExistingSolutionInitVM>().AsSelf();
            builder.RegisterType<ExistingProjectInitVM>().AsSelf();
            builder.RegisterType<NewSolutionInitVM>().AsSelf();
            builder.RegisterType<GitPatcherVM>().AsSelf();
            builder.RegisterType<SolutionPatcherVM>()
                .AsSelf()
                .AsImplementedInterfaces();
            builder.RegisterType<CliPatcherVM>().AsSelf();
            builder.RegisterType<PatchersRunVM>().AsSelf();
            
            // Test
            builder.RegisterType<PatcherSettingsVM>().AsSelf();
            
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
            
            builder.RegisterAssemblyTypes(typeof(PatcherVM).Assembly)
                .InNamespacesOf(
                    typeof(PatcherVM))
                .AsMatchingInterface();
            
            builder.RegisterAssemblyTypes(typeof(PatcherVM).Assembly)
                .InNamespacesOf(
                    typeof(ISolutionMetaFileSync))
                .SingleInstance()
                .AsMatchingInterface();
        }
    }
}