using DynamicData;
using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
