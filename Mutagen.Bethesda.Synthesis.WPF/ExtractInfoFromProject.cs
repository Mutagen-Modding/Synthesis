using System.IO.Abstractions;
using Path = System.IO.Path;
using System.Reflection;
using System.Runtime.Loader;
using Noggog;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Pathing;

namespace Mutagen.Bethesda.Synthesis.WPF;

public interface IExtractInfoFromProject
{
    Task<GetResponse<(TRet Item, TempFolder Temp)>> Extract<TRet>(
        TargetProject targetProject,
        CancellationToken cancel,
        Func<Assembly, GetResponse<TRet>> getter);
}

public class ExtractInfoFromProject : IExtractInfoFromProject
{
    private readonly IFileSystem _fileSystem;
    private readonly IWorkingDirectorySubPaths _paths;
    private readonly ICopyDirectory _copyDirectory;
    private readonly IQueryExecutablePath _queryExecutablePath;
    private readonly ILogger _logger;

    public ExtractInfoFromProject(
        IFileSystem fileSystem,
        IWorkingDirectorySubPaths paths,
        ICopyDirectory copyDirectory,
        IQueryExecutablePath queryExecutablePath,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _paths = paths;
        _copyDirectory = copyDirectory;
        _queryExecutablePath = queryExecutablePath;
        _logger = logger;
    }

    public async Task<GetResponse<(TRet Item, TempFolder Temp)>> Extract<TRet>(
        TargetProject targetProject,
        CancellationToken cancel,
        Func<Assembly, GetResponse<TRet>> getter)
    {
        if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");

        // Copy to a temp folder for build + loading, just to keep the main one free to be swapped/modified as needed
        var tempFolder = TempFolder.FactoryByPath(Path.Combine(_paths.LoadingFolder, Path.GetRandomFileName()));
        if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");
        var overallDir = Path.GetDirectoryName(targetProject.SolutionPath)!;
        _logger.Information("Starting project assembly info extraction.  Copying from {OverallDir} to {TempDirPath}",
            overallDir,
            tempFolder.Dir.Path);
        _copyDirectory.Copy(overallDir, tempFolder.Dir.Path, cancel);
        var projPath = Path.Combine(tempFolder.Dir.Path, targetProject.ProjSubPath);
        _logger.Information("Retrieving executable path from {ProjPath}", projPath);
        var exec = await _queryExecutablePath.Query(projPath, cancel).ConfigureAwait(false);
        if (exec.Failed) return exec.BubbleFailure<(TRet Item, TempFolder Temp)>();
        _logger.Information("Located executable path for {ProjPath}: {Result}", projPath, exec.Value);
        var ret = ExecuteAndUnload(exec.Value, getter);
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