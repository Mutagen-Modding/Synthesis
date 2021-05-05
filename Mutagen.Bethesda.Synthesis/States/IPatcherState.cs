using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;

namespace Mutagen.Bethesda.Synthesis
{
    /// <summary>
    /// An interface housing all the tools, parameters, and entry points for a typical Synthesis patcher
    /// </summary>
    public interface IPatcherState : IDisposable
    {
        /// <summary>
        /// Patch mod object to modify and make changes to.  The state of this object will be used to
        /// export the resulting patch for the next patcher in the pipeline. <br/>
        /// <br/>
        /// NOTE:  This object may come with existing records in it! <br/>
        /// Previous patchers in the pipeline will have already added content.  Your changes should build
        /// upon that content as appropriate, and mesh any changes to produce the final patch file.
        /// </summary>
        IModGetter PatchMod { get; }

        /// <summary>
        /// A list of ModKeys as they appeared, and whether they were enabled
        /// </summary>
        IReadOnlyList<LoadOrderListing> RawLoadOrder { get; }

        /// <summary>
        /// Cancellation token that signals whether to stop patching and exit early
        /// </summary>
        CancellationToken Cancel { get; }

        /// <summary>
        /// Path to the supplimental data folder dedicated to storing patcher specific user settings/files
        /// </summary>
        string ExtraSettingsDataPath { get; }

        /// <summary>
        /// Path to the default data folder as defined by the patcher's source code
        /// </summary>
        string? DefaultSettingsDataPath { get; }

        /// <summary>
        /// Path to the plugins.txt used
        /// </summary>
        string LoadOrderFilePath { get; }

        /// <summary>
        /// Path to the game data folder
        /// </summary>
        string DataFolderPath { get; }

        /// <summary>
        /// GameRelease targeted for patching
        /// </summary>
        GameRelease GameRelease { get; }

        /// <summary>
        /// Where Synthesis will eventually output the patch file.
        /// </summary>
        string OutputPath { get; }

        /// <summary>
        /// Where the patch output file from the previous patcher is located
        /// </summary>
        string? SourcePath { get; }

        /// <summary>
        /// A reference to the FormKey allocator assigned to PatchMod
        /// </summary>
        internal IFormKeyAllocator? FormKeyAllocator { get; }
    }

    public interface IPatcherState<TModSetter, TModGetter> : IPatcherState
        where TModSetter : class, IMod, TModGetter
        where TModGetter : class, IModGetter
    {
        /// <summary>
        /// Load Order object containing all the mods to be used for the patch.<br />
        /// This Load Order will contain the patch mod itself.  This reference is the same object
        /// as the PatchMod member, and so any modifications will implicitly be applied to the Load Order.
        /// </summary>
        LoadOrder<IModListing<TModGetter>> LoadOrder { get; }

        /// <summary>
        /// Convenience Link Cache to use created from the provided Load Order object.<br />
        /// The patch mod is marked as safe for mutation, and will not make the cache invalid.
        /// </summary>
        ILinkCache<TModSetter, TModGetter> LinkCache { get; }

        /// <summary>
        /// Patch mod object to modify and make changes to.  The state of this object will be used to
        /// export the resulting patch for the next patcher in the pipeline. <br/>
        /// <br/>
        /// NOTE:  This object may come with existing records in it! <br/>
        /// Previous patchers in the pipeline will have already added content.  Your changes should build
        /// upon that content as appropriate, and mesh any changes to produce the final patch file.
        /// </summary>
        new TModSetter PatchMod { get; }
    }

    public static class IPatcherStateExt
    {
        /// <summary>
        /// Attempts to locate and confirm the existence of a config file from either the user data folder
        /// or the default data folder as it exists in the source repository.
        /// </summary>
        /// <param name="state">Patcher state to refer to</param>
        /// <param name="relativePath">Path to the config file, relative to the data folder.</param>
        /// <param name="resolvedPath">Located config file that exists</param>
        /// <returns>True if config file was located that exists</returns>
        public static bool TryRetrieveConfigFile(this IPatcherState state, string relativePath, [MaybeNullWhen(false)] out string resolvedPath)
        {
            var userPath = Path.Combine(state.ExtraSettingsDataPath, relativePath);
            if (File.Exists(userPath))
            {
                resolvedPath = userPath;
                return true;
            }

            if (state.DefaultSettingsDataPath != null)
            {
                var defPath = Path.Combine(state.DefaultSettingsDataPath, relativePath);
                if (File.Exists(defPath))
                {
                    resolvedPath = defPath;
                    return true;
                }
            }

            resolvedPath = null;
            return false;
        }

        /// <summary>
        /// Locate and confirm the existence of a config file from either the user data folder
        /// or the default data folder as it exists in the source repository.
        /// </summary>
        /// <param name="state">Patcher state to refer to</param>
        /// <param name="relativePath">Path to the config file, relative to the data folder.</param>
        /// <exception cref="FileNotFoundException">If a config file could not be located that exists in either location.</exception>
        /// <returns>Located config file that exists</returns>
        public static string RetrieveConfigFile(this IPatcherState state, string relativePath)
        {
            if (TryRetrieveConfigFile(state, relativePath, out var resolved))
            {
                return resolved;
            }

            throw new FileNotFoundException($"Could not locate config file: {relativePath}");
        }
    }
}
