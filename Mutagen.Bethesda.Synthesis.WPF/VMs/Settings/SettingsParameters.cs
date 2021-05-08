using DynamicData;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.WPF.Plugins.Order;
using System;
using System.Reflection;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public record SettingsParameters(
        Assembly Assembly,
        IObservable<IChangeSet<LoadOrderEntryVM>> DetectedLoadOrder,
        IObservable<ILinkCache> LinkCache,
        Type TargetType,
        object? DefaultVal,
        ReflectionSettingsVM MainVM,
        SettingsNodeVM? Parent);
}
