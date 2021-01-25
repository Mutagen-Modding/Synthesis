using DynamicData;
using Mutagen.Bethesda;
using Newtonsoft.Json.Linq;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableModKeySettingsVM : SettingsNodeVM
    {
        private readonly IObservable<IChangeSet<LoadOrderEntryVM>> _detectedLoadOrder;
        private readonly ModKey[] _defaultVal;

        public RequiredModsVM AddedModsVM { get; }

        public EnumerableModKeySettingsVM(
            IObservable<IChangeSet<LoadOrderEntryVM>> detectedLoadOrder,
            string memberName,
            IEnumerable<ModKey> defaultVal)
            : base(memberName)
        {
            _defaultVal = defaultVal.ToArray();
            _detectedLoadOrder = detectedLoadOrder;
            AddedModsVM = new RequiredModsVM(detectedLoadOrder);
            AddedModsVM.RequiredMods.SetTo(defaultVal);
        }

        public static EnumerableModKeySettingsVM Factory(SettingsParameters param, string memberName, object? defaultVal)
        {
            return new EnumerableModKeySettingsVM(
                param.DetectedLoadOrder,
                memberName,
                defaultVal as IEnumerable<ModKey> ?? Enumerable.Empty<ModKey>());
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            AddedModsVM.RequiredMods.SetTo(
                property.EnumerateArray()
                .Select(elem => ModKeySettingsVM.Import(elem)));
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(AddedModsVM.RequiredMods.Items.Select(x => ModKeySettingsVM.Persist(x)).ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableModKeySettingsVM(_detectedLoadOrder, MemberName, _defaultVal);
        }
    }
}
