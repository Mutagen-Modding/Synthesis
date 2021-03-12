using Newtonsoft.Json.Linq;
using Noggog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class EnumerableEnumSettingsVM : EnumerableSettingsVM
    {
        private readonly string[] _defaultVal;
        private readonly string[] _enumNames;

        public EnumerableEnumSettingsVM(
            FieldMeta fieldMeta,
            string[] defaultVal,
            string[] enumNames)
            : base(fieldMeta,
                  get: null!,
                  add: coll => coll.Add(new ListElementWrapperVM<string, EnumSettingsVM>(new EnumSettingsVM(FieldMeta.Empty, enumNames[0], enumNames))
                  {
                      IsSelected = true
                  }))
        {
            _import = ImportSingle;
            _defaultVal = defaultVal;
            _enumNames = enumNames;
            Values.SetTo(defaultVal.Select(i =>
            {
                return new ListElementWrapperVM<string, EnumSettingsVM>(new EnumSettingsVM(FieldMeta.Empty, i, enumNames));
            }));
        }

        public static EnumerableEnumSettingsVM Factory(FieldMeta fieldMeta, object? defaultVal, Type enumType)
        {
            var names = Enum.GetNames(enumType).ToArray();
            var nameSet = names.ToHashSet();
            string[] defaults;
            if (defaultVal is IEnumerable items)
            {
                var strs = new List<string>();
                foreach (var obj in items)
                {
                    var str = obj?.ToString();
                    if (str != null && nameSet.Contains(str))
                    {
                        strs.Add(str);
                    }
                }
                defaults = strs.ToArray();
            }
            else
            {
                defaults = Array.Empty<string>();
            }
            return new EnumerableEnumSettingsVM(fieldMeta, defaults, names);
        }

        private TryGet<IBasicSettingsNodeVM> ImportSingle(JsonElement elem)
        {
            var str = elem.ToString();
            if (str != null && _enumNames.Contains(str))
            {
                return TryGet<IBasicSettingsNodeVM>.Succeed(
                    new ListElementWrapperVM<string, EnumSettingsVM>(new EnumSettingsVM(FieldMeta.Empty, str, _enumNames)));
            }
            return TryGet<IBasicSettingsNodeVM>.Failure;
        }

        public override void Import(JsonElement property, Action<string> logger)
        {
            Values.Clear();
            foreach (var elem in property.EnumerateArray())
            {
                var str = elem.ToString();
                if (str != null && _enumNames.Contains(str))
                {
                    Values.Add(new ListElementWrapperVM<string, EnumSettingsVM>(new EnumSettingsVM(FieldMeta.Empty, str, _enumNames)));
                }
            }
        }

        public override void Persist(JObject obj, Action<string> logger)
        {
            obj[Meta.DiskName] = new JArray(Values
                .Select(x => ((IBasicSettingsNodeVM)x.Value).Value)
                .ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableEnumSettingsVM(Meta, _defaultVal, _enumNames);
        }
    }
}
