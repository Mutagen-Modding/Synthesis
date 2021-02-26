using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Synthesis.Bethesda.GUI
{
    public class EnumDictionarySettingsVM : ADictionarySettingsVM
    {
        public EnumDictionarySettingsVM(FieldMeta fieldMeta, KeyValuePair<string, SettingsNodeVM>[] values, SettingsNodeVM prototype)
            : base(fieldMeta, values, prototype)
        {
        }

        public static EnumDictionarySettingsVM Factory(SettingsParameters param, FieldMeta fieldMeta, Type enumType)
        {
            var vals = GetDefaultValDictionary(param.DefaultVal);
            var proto = SettingsNodeVM.MemberFactory(param with { DefaultVal = null }, member: null);
            proto.WrapUp();
            return new EnumDictionarySettingsVM(
                fieldMeta,
                Enum.GetNames(enumType).Select(e =>
                {
                    if (!vals.TryGetValue(e, out var defVal))
                    {
                        defVal = null;
                    }
                    return new KeyValuePair<string, SettingsNodeVM>(
                        e,
                        SettingsNodeVM.MemberFactory(param with { DefaultVal = defVal }, member: null));
                }).ToArray(),
                proto);
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumDictionarySettingsVM(Meta, _values, _prototype);
        }
    }
}
