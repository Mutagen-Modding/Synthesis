using System;
using System.IO.Abstractions;
using Path = System.IO.Path;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public interface IExtractInfoFromProject
    {
        Task<GetResponse<(TRet Item, TempFolder Temp)>> Extract<TRet>(string projPath, CancellationToken cancel, Func<Assembly, GetResponse<TRet>> getter);
    }

    public class ExtractInfoFromProject : IExtractInfoFromProject
    {
        private readonly IFileSystem _FileSystem;
        private readonly IPaths _Paths;
        private readonly ICopyDirectory _CopyDirectory;
        private readonly IQueryExecutablePath _QueryExecutablePath;
        private readonly ILogger _Logger;

        public ExtractInfoFromProject(
            IFileSystem fileSystem,
            IPaths paths,
            ICopyDirectory copyDirectory,
            IQueryExecutablePath queryExecutablePath,
            ILogger logger)
        {
            _FileSystem = fileSystem;
            _Paths = paths;
            _CopyDirectory = copyDirectory;
            _QueryExecutablePath = queryExecutablePath;
            _Logger = logger;
        }

        public async Task<GetResponse<(TRet Item, TempFolder Temp)>> Extract<TRet>(
            string projPath,
            CancellationToken cancel,
            Func<Assembly, GetResponse<TRet>> getter)
        {
            if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");

            // Copy to a temp folder for build + loading, just to keep the main one free to be swapped/modified as needed
            var tempFolder = TempFolder.FactoryByPath(Path.Combine(_Paths.LoadingFolder, Path.GetRandomFileName()));
            if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");
            var projDir = Path.GetDirectoryName(projPath)!;
            _Logger.Information($"Starting project assembly info extraction.  Copying project from {projDir} to {tempFolder.Dir.Path}");
            _CopyDirectory.Copy(projDir, tempFolder.Dir.Path, cancel);
            projPath = Path.Combine(tempFolder.Dir.Path, Path.GetFileName(projPath));
            _Logger.Information($"Retrieving executable path from {projPath}");
            var exec = await _QueryExecutablePath.Query(projPath, cancel);
            if (exec.Failed) return exec.BubbleFailure<(TRet Item, TempFolder Temp)>();
            _Logger.Information($"Located executable path for {projPath}: {exec.Value}");
            var ret = ExecuteAndUnload(exec.Value, getter);
            if (ret.Failed) return ret.BubbleFailure<(TRet Item, TempFolder Temp)>();
            return (ret.Value, tempFolder);
        }
        
        private GetResponse<TRet> ExecuteAndUnload<TRet>(string exec, Func<Assembly, GetResponse<TRet>> getter)
        {
            return AssemblyLoading.ExecuteAndForceUnload(exec, getter, () => new FormKeyAssemblyLoadContext(_FileSystem, exec));
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
}