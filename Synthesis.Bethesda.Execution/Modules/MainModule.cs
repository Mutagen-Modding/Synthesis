using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;
using Synthesis.Bethesda.Execution.FileAssociations;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Running.Cli;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.Execution.Modules;

public class MainModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(IQueryNewestLibraryVersions),
                typeof(IProcessRunner),
                typeof(IWorkingDirectorySubPaths),
                typeof(IPatcherRun),
                typeof(IInstalledSdkFollower),
                typeof(IConsiderPrereleasePreference),
                typeof(IPatcherNameSanitizer),
                typeof(ILinesToReflectionConfigsParser),
                typeof(INugetErrorSolution),
                typeof(IRunProfileProvider),
                typeof(ExportGitAddFile),
                typeof(IProjectRunProcessStartInfoProvider),
                typeof(IBuild))
            .NotInNamespacesOf(typeof(IInstalledSdkFollower))
            .TypicalRegistrations()
            .AsSelf();
            
        builder.RegisterAssemblyTypes(typeof(IWorkDropoff).Assembly)
            .InNamespacesOf(
                typeof(IWorkDropoff))
            .SingleInstance()
            .AsImplementedInterfaces();
            
        builder.RegisterAssemblyTypes(typeof(IProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(IExecuteRun))
            .InstancePerMatchingLifetimeScope(LifetimeScopes.RunNickname)
            .TypicalRegistrations();
            
        builder.RegisterAssemblyTypes(typeof(IProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(IInstalledSdkFollower))
            .SingleInstance()
            .AsMatchingInterface();
            
        builder.RegisterAssemblyTypes(typeof(IProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(ICheckOrCloneRepo))
            .SingleInstance()
            .AsMatchingInterface();
    }
}