using System.IO.Abstractions;
using Path = System.IO.Path;
using System.Reflection;
using System.Runtime.Loader;
using Noggog;
using Noggog.IO;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Pathing;

namespace Mutagen.Bethesda.Synthesis.WPF;

public interface IExtractInfoFromProject
{
    Task<GetResponse<(TRet Item, TempFolder Temp)>> Extract<TRet>(
        TargetProject targetProject,
        string prebuiltExecutablePath,
        CancellationToken cancel,
        Func<Assembly, GetResponse<TRet>> getter);
}

public class ExtractInfoFromProject : IExtractInfoFromProject
{
    private readonly IFileSystem _fileSystem;
    private readonly IWorkingDirectorySubPaths _paths;
    private readonly ICopyDirectory _copyDirectory;
    private readonly ILogger _logger;

    public ExtractInfoFromProject(
        IFileSystem fileSystem,
        IWorkingDirectorySubPaths paths,
        ICopyDirectory copyDirectory,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _paths = paths;
        _copyDirectory = copyDirectory;
        _logger = logger;
    }

    public async Task<GetResponse<(TRet Item, TempFolder Temp)>> Extract<TRet>(
        TargetProject targetProject,
        string prebuiltExecutablePath,
        CancellationToken cancel,
        Func<Assembly, GetResponse<TRet>> getter)
    {
        if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");

        // Load from a throwaway copy: the assembly stays locked for the lifetime of the settings panel,
        // and that lock must not land on the live runner directory.
        var tempFolder = TempFolder.FactoryByPath(Path.Combine(_paths.LoadingFolder, Path.GetRandomFileName()));
        if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");
        var overallDir = Path.GetDirectoryName(targetProject.SolutionPath)!;
        _logger.Information("Starting project assembly info extraction.  Copying from {OverallDir} to {TempDirPath}",
            overallDir,
            tempFolder.Dir.Path);
        _copyDirectory.Copy(overallDir, tempFolder.Dir.Path, cancel);

        var relative = Path.GetRelativePath(overallDir, prebuiltExecutablePath);
        var copiedExec = Path.Combine(tempFolder.Dir.Path, relative);
        if (!_fileSystem.File.Exists(copiedExec))
        {
            _logger.Error(
                "Prebuilt executable {Prebuilt} was not found in the copied project at {Copied}; " +
                "the previous build likely had issues.  Refusing to build during settings open",
                prebuiltExecutablePath, copiedExec);
            return GetResponse<(TRet Item, TempFolder Temp)>.Fail(
                $"Prebuilt executable was not found in the copied project: {copiedExec}");
        }

        _logger.Information("Using prebuilt executable for settings extraction: {Prebuilt} -> {Copied}",
            prebuiltExecutablePath, copiedExec);
        var ret = ExecuteAndUnload(copiedExec, getter);
        if (ret.Failed) return ret.BubbleFailure<(TRet Item, TempFolder Temp)>();
        return (ret.Value, tempFolder);
    }

    private GetResponse<TRet> ExecuteAndUnload<TRet>(string exec, Func<Assembly, GetResponse<TRet>> getter)
    {
        return AssemblyLoading.ExecuteAndForceUnload(exec, getter, () => new FormKeyAssemblyLoadContext(_fileSystem, exec));
    }

    class FormKeyAssemblyLoadContext : AssemblyLoadContext
    {
        // Resolver of the locations of the assemblies that are dependencies of the
        // main plugin assembly.
        private readonly AssemblyDependencyResolver _resolver;

        public FormKeyAssemblyLoadContext(
            IFileSystem fileSystem,
            string pluginPath) 
            : base(isCollectible: true)
        {
            if (!fileSystem.File.Exists(pluginPath)) throw new System.IO.FileNotFoundException($"Assembly path to resolve against didn't exist: {pluginPath}");
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        // The Load method override causes all the dependencies present in the plugin's binary directory to get loaded
        // into the HostAssemblyLoadContext together with the plugin assembly itself.
        // NOTE: The Interface assembly must not be present in the plugin's binary directory, otherwise we would
        // end up with the assembly being loaded twice. Once in the default context and once in the HostAssemblyLoadContext.
        // The types present on the host and plugin side would then not match even though they would have the same names.
        protected override Assembly? Load(AssemblyName name)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(name);

            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
}