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
            var ret = AssemblyLoading.ExecuteAndUnload(exec.Value, out hostAlcWeakRef, getter);
            for (int i = 0; hostAlcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return ret;
        }
    }
}
