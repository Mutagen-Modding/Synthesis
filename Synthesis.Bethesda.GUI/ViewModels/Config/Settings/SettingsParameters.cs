using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public record SettingsParameters(
        Assembly Assembly,
        IObservable<IChangeSet<LoadOrderEntryVM>> DetectedLoadOrder);
}
