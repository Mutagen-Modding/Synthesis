using Mutagen.Bethesda.Synthesis.CLI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Mutagen.Bethesda.Synthesis
{
    /// <summary>
    /// An interface housing all the tools, parameters, and entry points for a typical Synthesis patcher
    /// </summary>
    public interface IPatcherState : IDisposable
    {
        /// <summary>
        /// Instructions given to the patcher from the Synthesis pipeline
        /// </summary>
        RunSynthesisMutagenPatcher Settings { get; }

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
        /// Path to the supplimental data folder dedicated to storing patcher specific settings/files
        /// </summary>
        string ExtraSettingsDataPath { get; }
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
        ILinkCache<TModSetter> LinkCache { get; }

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
}
