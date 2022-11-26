using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;

namespace Mutagen.Bethesda.Synthesis;

/// <summary>
/// An interface housing all the tools, parameters, and entry points for a typical Synthesis patcher
/// </summary>
public interface IPatcherState : IBaseRunState
{
    /// <summary>
    /// Patch mod object to modify and make changes to.  The state of this object will be used to
    /// export the resulting patch for the next patcher in the pipeline. <br/>
    /// <br/>
    /// NOTE:  This object may come with existing records in it! <br/>
    /// Previous patchers in the pipeline will have already added content.  Your changes should build
    /// upon that content as appropriate, and mesh any changes to produce the final patch file.
    /// </summary>
    IMod PatchMod { get; }

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

public interface IPatcherState<TModSetter, TModGetter> : IBaseRunState<TModSetter, TModGetter>, IPatcherState
    where TModSetter : class, IMod, TModGetter
    where TModGetter : class, IModGetter
{
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