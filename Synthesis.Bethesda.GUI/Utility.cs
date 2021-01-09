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

        public static async Task<GetResponse<Type>> ExtractTypeFromApp(string projPath, string targetType, CancellationToken cancel)
        {
            var exec = await DotNetCommands.GetExecutablePath(projPath, cancel);
            if (exec.Failed) return exec.BubbleFailure<Type>();
            var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
            try
            {
                var assemb = context.LoadFromAssemblyPath(exec.Value);
                var type = assemb.GetType(targetType);
                if (type == null)
                {
                    return GetResponse<Type>.Fail($"Could not find type {targetType} in assembly {exec.Value} from project {projPath}");
                }
                return GetResponse<Type>.Succeed(type);
            }
            catch (Exception ex)
            {
                return GetResponse<Type>.Fail(ex);
            }
            finally
            {
                context.Unload();
            }
        }
    }
}
