using Buildalyzer;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public static class Utility
    {
        public static async void NavigateToPath(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error navigating to path: {path}");
            }
        }

        public static void CopyDirectory(string source, string target, CancellationToken cancel)
        {
            var stack = new Stack<Folders>();
            stack.Push(new Folders(source, target));

            while (stack.Count > 0)
            {
                if (cancel.IsCancellationRequested) return;
                var folders = stack.Pop();
                Directory.CreateDirectory(folders.Target);
                foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
                {
                    if (cancel.IsCancellationRequested) return;
                    File.Copy(file, Path.Combine(folders.Target, Path.GetFileName(file)));
                }

                foreach (var folder in Directory.GetDirectories(folders.Source))
                {
                    if (cancel.IsCancellationRequested) return;
                    stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }
        }

        public class Folders
        {
            public string Source { get; private set; }
            public string Target { get; private set; }

            public Folders(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }

        public static async Task<GetResponse<(TRet Item, TempFolder Temp)>> ExtractInfoFromProject<TRet>(string projPath, CancellationToken cancel, Func<Assembly, GetResponse<TRet>> getter, Action<string> log)
        {
            if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");

            // Copy to a temp folder for build + loading, just to keep the main one free to be swapped/modified as needed
            var tempFolder = TempFolder.FactoryByPath(Path.Combine(Paths.LoadingFolder, Path.GetRandomFileName()));
            if (cancel.IsCancellationRequested) return GetResponse<(TRet Item, TempFolder Temp)>.Fail("Cancelled");
            var projDir = Path.GetDirectoryName(projPath)!;
            log($"Starting project assembly info extraction.  Copying project from {projDir} to {tempFolder.Dir.Path}");
            CopyDirectory(projDir, tempFolder.Dir.Path, cancel);
            projPath = Path.Combine(tempFolder.Dir.Path, Path.GetFileName(projPath));
            log($"Retrieving executable path from {projPath}");
            var exec = await DotNetCommands.GetExecutablePath(projPath, cancel, log);
            if (exec.Failed) return exec.BubbleFailure<(TRet Item, TempFolder Temp)>();
            log($"Located executable path for {projPath}: {exec.Value}");
            var ret = ExecuteAndUnload(exec.Value, getter);
            if (ret.Failed) return ret.BubbleFailure<(TRet Item, TempFolder Temp)>();
            return (ret.Value, tempFolder);
        }

        private static GetResponse<TRet> ExecuteAndUnload<TRet>(string exec, Func<Assembly, GetResponse<TRet>> getter)
        {
            return AssemblyLoading.ExecuteAndForceUnload(exec, getter, () => new FormKeyAssemblyLoadContext(exec));
        }

        class FormKeyAssemblyLoadContext : AssemblyLoadContext
        {
            // Resolver of the locations of the assemblies that are dependencies of the
            // main plugin assembly.
            private readonly AssemblyDependencyResolver _resolver;

            public FormKeyAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
            {
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

                // Only load formkey libs, for now
                if (!name.Name?.StartsWith("Mutagen.Bethesda.FormKeys") ?? true) return null;

                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
            }
        }
    }
}
