using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public static class ReflectionUtility
    {
        static void CopyDirectory(string source, string target, CancellationToken cancel)
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

        class Folders
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
                if (!File.Exists(pluginPath)) throw new FileNotFoundException($"Assembly path to resolve against didn't exist: {pluginPath}");
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

        public static bool TryGetCustomAttributeByName(this MemberInfo info, string name, [MaybeNullWhen(false)] out Attribute attr)
        {
            attr = Attribute.GetCustomAttributes(info).FirstOrDefault(a => a.GetType().Name == name);
            return attr != null;
        }

        public static T GetCustomAttributeValueByName<T>(this MemberInfo info, string attrName, string valName, T fallback)
        {
            if (!TryGetCustomAttributeByName(info, attrName, out var attr)) return fallback;
            var propInfo = attr.GetType().GetProperty(valName);
            if (propInfo == null) return fallback;
            return (T)propInfo.GetValue(attr)!;
        }

        public static IEnumerable<Attribute> GetCustomAttributesByName(this MemberInfo info, string name)
        {
            return Attribute.GetCustomAttributes(info).Where(a => a.GetType().Name == name);
        }

        /// <summary>
        /// Helps to get properties in inherited interfaces
        /// </summary>
        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
        {
            if (!type.IsInterface)
                return type.GetProperties();

            return (new Type[] { type })
                   .Concat(type.GetInterfaces())
                   .SelectMany(i => i.GetProperties());
        }
    }
}
