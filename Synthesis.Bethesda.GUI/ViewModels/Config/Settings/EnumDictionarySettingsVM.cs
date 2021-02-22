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
    public class EnumDictionaryItem
    {
        public string Key { get; }
        public SettingsNodeVM Value { get; }

        public EnumDictionaryItem(string key, SettingsNodeVM value)
        {
            Key = key;
            Value = value;
        }
    }

    public class EnumDictionarySettingsVM : SettingsNodeVM
    {
        private readonly KeyValuePair<string, SettingsNodeVM>[] _values;
        public ObservableCollection<EnumDictionaryItem> Items { get; } = new ObservableCollection<EnumDictionaryItem>();

        [Reactive]
        public EnumDictionaryItem? Selected { get; set; }

        public EnumDictionarySettingsVM(string memberName, KeyValuePair<string, SettingsNodeVM>[] values)
            : base(memberName)
        {
            _values = values;
            Items.SetTo(values.Select(e => new EnumDictionaryItem(e.Key, e.Value.Duplicate())));
            Selected = Items.FirstOrDefault();
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            throw new NotImplementedException();
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            foreach (var item in Items)
            {
                var subObj = new JObject();
                item.Value.Persist(subObj, logger);
                obj[item.Key] = subObj;
            }
        }

        public static EnumDictionarySettingsVM Factory(SettingsParameters param, string memberName, Type enumType, Type valType, object? defaultVals)
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();
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
                    vals[keyProp.GetValue(item)!.ToString() ?? string.Empty] = valProp!.GetValue(item)!;
                }
            }
            return new EnumDictionarySettingsVM(
                memberName,
                Enum.GetNames(enumType).Select(e =>
                {
                    if (!vals.TryGetValue(e, out var defVal))
                    {
                        defVal = null;
                    }
                    return new KeyValuePair<string, SettingsNodeVM>(
                        e,
                        SettingsNodeVM.MemberFactory(param, member: null, targetType: valType, defaultVal: defVal));
                }).ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumDictionarySettingsVM(MemberName, _values);
        }

        public override void WrapUp()
        {
            base.WrapUp();
            _values.ForEach(i => i.Value.WrapUp());
            Items.ForEach(i => i.Value.WrapUp());
        }
    }
}
