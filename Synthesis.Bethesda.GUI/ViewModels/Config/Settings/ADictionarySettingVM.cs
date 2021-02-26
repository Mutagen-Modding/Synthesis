using Newtonsoft.Json.Linq;
using Noggog;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class DictionarySettingItemVM
    {
        public string Key { get; }
        public SettingsNodeVM Value { get; }

        public DictionarySettingItemVM(string key, SettingsNodeVM value)
        {
            Key = key;
            Value = value;
        }
    }

    public abstract class ADictionarySettingsVM : SettingsNodeVM
    {
        protected readonly KeyValuePair<string, SettingsNodeVM>[] _values;
        protected readonly SettingsNodeVM _prototype;

        public ObservableCollection<DictionarySettingItemVM> Items { get; } = new ObservableCollection<DictionarySettingItemVM>();

        [Reactive]
        public DictionarySettingItemVM? Selected { get; set; }

        public ADictionarySettingsVM(FieldMeta fieldMeta, KeyValuePair<string, SettingsNodeVM>[] values, SettingsNodeVM prototype)
            : base(fieldMeta)
        {
            _values = values;
            _prototype = prototype;
            Items.SetTo(values.Select(e => new DictionarySettingItemVM(e.Key, e.Value.Duplicate())));
            Selected = Items.FirstOrDefault();
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Items.SetTo(property.EnumerateObject().Select(obj =>
            {
                var settingVM = _prototype.Duplicate();
                settingVM.WrapUp();
                settingVM.Import(obj.Value, logger);
                return new DictionarySettingItemVM(obj.Name, settingVM);
            }));
            Selected = Items.FirstOrDefault();
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            var dictObj = new JObject();
            obj[Meta.DiskName] = dictObj;
            foreach (var item in Items)
            {
                item.Value.Meta = FieldMeta.Empty with { DiskName = item.Key };
                item.Value.Persist(dictObj, logger);
            }
        }

        public static Dictionary<string, object> GetDefaultValDictionary(object? defaultVals)
        {
            var vals = new Dictionary<string, object>();
            // KeyValuePair doesn't implement an interface, so covariance can't help us here
            // Using `dynamic` keeps the target temporary types in memory (presumedly in its internal caches)
            // So we're doing normal reflection to retrieve the values, for now
            if (defaultVals is IEnumerable e)
            {
                PropertyInfo? keyProp = null;
                PropertyInfo? valProp = null;
                foreach (var item in e)
                {
                    keyProp ??= item.GetType().GetProperty("Key")!;
                    valProp ??= item.GetType().GetProperty("Value")!;
                    vals[keyProp.GetValue(item)!.ToString()!] = valProp!.GetValue(item)!;
                }
            }
            return vals;
        }

        public override void WrapUp()
        {
            base.WrapUp();
            _values.ForEach(i => i.Value.WrapUp());
            Items.ForEach(i => i.Value.WrapUp());
        }
    }
}
