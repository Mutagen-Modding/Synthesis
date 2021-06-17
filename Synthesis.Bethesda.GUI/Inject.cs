using System;
using System.IO.Abstractions;
using System.Linq;
using Mutagen.Bethesda.Installs;
using Mutagen.Bethesda.Synthesis.Versioning;
using Mutagen.Bethesda.Synthesis.WPF;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using Serilog;
using StructureMap;
using StructureMap.Graph;
using StructureMap.Graph.Scanning;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.GUI.Profiles.Plugins;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Services.Singletons;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI
{
    public class Inject
    {
        private ConfigurationExpression _coll = null!;
        public static Container Container { get; private set; } = null!;

        public Inject(Action<ConfigurationExpression> toAdd)
        {
            Container = new Container(c =>
            {
                _coll = c;
                Configure();
                toAdd(c);
            });
            var logging = Container.GetInstance<ILogger>();
// #if DEBUG
//             Container.AssertConfigurationIsValid();
//             logging.Information(Container.WhatDidIScan());
//             logging.Information(Container.WhatDoIHave());
// #endif
        }
        
        private void Configure()
        {
            RegisterBaseLib();
            RegisterCurrentLib();
            RegisterWpfLib();
            RegisterExecutionLib();
            RegisterOther();
            RegisterMutagen();
            RegisterCSharpExt();
        }

        private void RegisterCurrentLib()
        {
            _coll.ForSingletonOf<MainVM>();
            _coll.ForSingletonOf<ConfigurationVM>();
            _coll.ForSingletonOf<PatcherInitializationVM>();
            _coll.ForSingletonOf<ILogger>().Use(Log.Logger);
            _coll.ForSingletonOf<ISettingsSingleton>().Use<SettingsSingleton>();
            _coll.ForSingletonOf<IShowHelpSetting>().Use<ShowHelpSetting>();
            _coll.ForSingletonOf<IConsiderPrereleasePreference>().Use<ConsiderPrereleasePreference>();
            _coll.ForSingletonOf<IConfirmationPanelControllerVm>().Use<ConfirmationPanelControllerVm>();
            _coll.ForSingletonOf<ISelectedProfileControllerVm>().Use<SelectedProfileControllerVm>();
            _coll.ForSingletonOf<IActivePanelControllerVm>().Use<ActivePanelControllerVm>();
            _coll.ForSingletonOf<RetrieveSaveSettings>();
            _coll.Forward<RetrieveSaveSettings, IRetrieveSaveSettings>();
            _coll.Forward<RetrieveSaveSettings, ISaveSignal>();
            
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<IEnvironmentErrorVM>();
                s.AddAllTypesOf<IEnvironmentErrorVM>();
            });
            
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<INavigateTo>();
                s.IncludeNamespaceContainingType<INavigateTo>();
                s.ExcludeNamespaceContainingType<IInitilize>();
                s.WithDefaultConventions();
            });
            
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<IInitilize>();
                s.IncludeNamespaceContainingType<IInitilize>();
                s.Convention<SingletonConvention>();
            });
            
            _coll.For<ILockToCurrentVersioning>().Use<LockToCurrentVersioning>();
            _coll.For<IProfileDisplayControllerVm>().Use<ProfileDisplayControllerVm>();
            _coll.For<IEnvironmentErrorsVM>().Use<EnvironmentErrorsVM>();
            _coll.For<ProfilePatchersList>();
            _coll.Forward<ProfilePatchersList, IRemovePatcherFromProfile>();
            _coll.Forward<ProfilePatchersList, IProfilePatchersList>();
            _coll.For<IProfileIdentifier>().Use<ProfileIdentifier>();
            _coll.For<IProfileLoadOrder>().Use<ProfileLoadOrder>();
            _coll.For<IProfileDirectories>().Use<ProfileDirectories>();
            _coll.For<IProfileDataFolder>().Use<ProfileDataFolder>();
            _coll.For<IProfileVersioning>().Use<ProfileVersioning>();
            _coll.For<IProfileSimpleLinkCache>().Use<ProfileSimpleLinkCache>();
            _coll.For<GitPatcherInitVM>();
            _coll.For<CliPatcherInitVM>();
            
            // Overrides
            _coll.ForSingletonOf<IProvideWorkingDirectory>().Use<WorkingDirectoryOverride>();
        }

        private void RegisterOther()
        {
            _coll.For<IFileSystem>().Use<FileSystem>();
        }

        private void RegisterMutagen()
        {
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<IGameLocator>();
                s.WithDefaultConventions();
            });
        }

        private void RegisterCSharpExt()
        {
            _coll.For<ISchedulerProvider>().Use<SchedulerProvider>();
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<IWatchFile>();
                s.IncludeNamespaceContainingType<IWatchFile>();
                s.WithDefaultConventions();
            });
        }

        private void RegisterBaseLib()
        {
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<IProvideCurrentVersions>(); 
                s.IncludeNamespaceContainingType<IProvideCurrentVersions>();
                s.Convention<SingletonConvention>();
            });
        }

        private void RegisterWpfLib()
        {
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<IProvideAutogeneratedSettings>();
                s.IncludeNamespaceContainingType<IProvideAutogeneratedSettings>();
                s.WithDefaultConventions();
            });
        }

        private void RegisterExecutionLib()
        {
            _coll.Scan(s =>
            {
                s.AssemblyContainingType<ICheckOrCloneRepo>();
                s.ExcludeType<ProvideWorkingDirectory>();
                s.Convention<SingletonConvention>();
            });
        }
        
        internal class SingletonConvention : IRegistrationConvention
        {
            public void ScanTypes(TypeSet types, Registry registry)
            {
                // Only work on concrete types
                types.FindTypes(TypeClassification.Concretes | TypeClassification.Closed).ForEach(type =>
                {
                    // Register against all the interfaces implemented
                    // by this concrete class
                    type.GetInterfaces()
                        .Where(i => i.Name == $"I{type.Name}")
                        .ForEach(i => registry.For(i).Use(type).Singleton());
                });
            }
        }
    }
}