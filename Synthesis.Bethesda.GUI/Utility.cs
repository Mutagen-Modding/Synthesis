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
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public static class Utility
    {
        public static IEnumerable<string> AvailableProjectSubpaths(string solutionPath)
        {
            if (!File.Exists(solutionPath)) return Enumerable.Empty<string>();
            try
            {
                var manager = new AnalyzerManager(solutionPath);
                return manager.Projects.Keys.Select(projPath => projPath.TrimStart($"{Path.GetDirectoryName(solutionPath)}\\"!));
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }

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

        class HostAssemblyLoadContext : AssemblyLoadContext
        {
            // Resolver of the locations of the assemblies that are dependencies of the
            // main plugin assembly.
            private AssemblyDependencyResolver _resolver;

            public HostAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
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
                if (assemblyPath != null)
                {
                    Console.WriteLine($"Loading assembly {assemblyPath} into the HostAssemblyLoadContext");
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
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

        public static async Task<GetResponse<TRet>> ExtractInfoFromProject<TRet>(string projPath, CancellationToken cancel, Func<Assembly, GetResponse<TRet>> getter)
        {
            if (cancel.IsCancellationRequested) return GetResponse<TRet>.Fail("Cancelled");

            // We copy to a temp folder, as despite all the hoops jumped through to unload the assembly,
            // it still seems to lock the dll files.  For whatever reason, though, deleting the folder
            // containing all those files seems to work out? This is definitely a hack.  Unload should
            // ideally just work out of the box.
            using var tempFolder = new TempFolder(Path.Combine(Synthesis.Bethesda.Execution.Constants.WorkingDirectory, "Loading", Path.GetRandomFileName()));
            if (cancel.IsCancellationRequested) return GetResponse<TRet>.Fail("Cancelled");
            CopyDirectory(Path.GetDirectoryName(projPath)!, tempFolder.Dir.Path, cancel);
            projPath = Path.Combine(tempFolder.Dir.Path, Path.GetFileName(projPath));
            var exec = await DotNetCommands.GetExecutablePath(projPath, cancel);
            if (exec.Failed) return exec.BubbleFailure<TRet>();
            WeakReference hostAlcWeakRef;
            var ret = ExecuteAndUnload(exec.Value, out hostAlcWeakRef, getter);
            for (int i = 0; hostAlcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return ret;
        }

        // It is important to mark this method as NoInlining, otherwise the JIT could decide
        // to inline it into the Main method. That could then prevent successful unloading
        // of the plugin because some of the MethodInfo / Type / Plugin.Interface / HostAssemblyLoadContext
        // instances may get lifetime extended beyond the point when the plugin is expected to be
        // unloaded.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static GetResponse<TRet> ExecuteAndUnload<TRet>(string assemblyPath, out WeakReference alcWeakRef, Func<Assembly, GetResponse<TRet>> getter)
        {
            // Create the unloadable HostAssemblyLoadContext
            var alc = new HostAssemblyLoadContext(assemblyPath);

            // Create a weak reference to the AssemblyLoadContext that will allow us to detect
            // when the unload completes.
            alcWeakRef = new WeakReference(alc);

            try
            {
                // Load the plugin assembly into the HostAssemblyLoadContext.
                // NOTE: the assemblyPath must be an absolute path.
                Assembly assemb = alc.LoadFromAssemblyPath(assemblyPath);
                return getter(assemb);
            }
            catch (OperationCanceledException)
            {
                return GetResponse<TRet>.Fail("Cancelled");
            }
            finally
            {
                // This initiates the unload of the HostAssemblyLoadContext. The actual unloading doesn't happen
                // right away, GC has to kick in later to collect all the stuff.
                alc.Unload();
            }
        }
    }
}
