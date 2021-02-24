using Newtonsoft.Json.Linq;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class EnumSettingsVM : SettingsNodeVM, IBasicSettingsNodeVM
    {
        private string? _defaultVal;

        public IEnumerable<string> EnumNames { get; }

        [Reactive]
        public string Value { get; set; }

        object IBasicSettingsNodeVM.Value => this.Value!;

        [Reactive]
        public bool IsSelected { get; set; }

        public EnumSettingsVM(MemberName memberName, string? defaultVal, IEnumerable<string> enumNames)
            : base(memberName)
        {
            EnumNames = enumNames;
            _defaultVal = defaultVal;
            Value = defaultVal ?? string.Empty;
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Value = property.GetString() ?? string.Empty;
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName.DiskName] = JToken.FromObject(Value);
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumSettingsVM(MemberName, _defaultVal, EnumNames);
        }

        public static EnumSettingsVM Factory(MemberName memberName, object? defaultVal, Type enumType)
        {
            var names = Enum.GetNames(enumType).ToArray();
            return new EnumSettingsVM(memberName, defaultVal?.ToString(), names);
        }
    }
}
