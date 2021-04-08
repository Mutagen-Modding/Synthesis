using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.WPF;
using Newtonsoft.Json.Linq;
using Noggog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class EnumerableModKeySettingsVM : SettingsNodeVM
    {
        private readonly ModKey[] _defaultVal;
        public ObservableCollection<ModKeyItemViewModel> Values { get; } = new ObservableCollection<ModKeyItemViewModel>();
        public IObservable<IChangeSet<ModKey>> DetectedLoadOrder { get; }

        public EnumerableModKeySettingsVM(
            IObservable<IChangeSet<ModKey>> detectedLoadOrder,
            FieldMeta fieldMeta,
            IEnumerable<ModKey> defaultVal)
            : base(fieldMeta)
        {
            _defaultVal = defaultVal.ToArray();
            DetectedLoadOrder = detectedLoadOrder;
            Values.SetTo(defaultVal.Select(i => new ModKeyItemViewModel(i)));
        }

        public static EnumerableModKeySettingsVM Factory(SettingsParameters param, FieldMeta fieldMeta, object? defaultVal)
        {
            var defaultKeys = new List<ModKey>();
            if (defaultVal is IEnumerable e)
            {
                foreach (var item in e)
                {
                    defaultKeys.Add(ModKey.FromNameAndExtension(item.ToString()));
                }
            }
            return new EnumerableModKeySettingsVM(
                param.DetectedLoadOrder.Transform(x => x.Listing.ModKey),
                fieldMeta,
                defaultKeys);
        }

        public override void Import(JsonElement property, Action<string> logger)
        {
            Values.Clear();
            foreach (var elem in property.EnumerateArray())
            {
                if (ModKey.TryFromNameAndExtension(elem.GetString(), out var modKey))
                {
                    Values.Add(new ModKeyItemViewModel(modKey));
                }
                else
                {
                    Values.Add(new ModKeyItemViewModel(ModKey.Null));
                }
            }
        }

        public override void Persist(JObject obj, Action<string> logger)
        {
            obj[Meta.DiskName] = new JArray(Values
                .Select(x =>
                {
                    if (x.ModKey.IsNull)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return x.ModKey.ToString();
                    }
                }).ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableModKeySettingsVM(DetectedLoadOrder, Meta, _defaultVal);
        }
    }
}
