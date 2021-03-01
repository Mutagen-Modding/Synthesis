using Newtonsoft.Json.Linq;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public string DisplayName => _DisplayName.Value;

        public EnumSettingsVM(FieldMeta fieldMeta, string? defaultVal, IEnumerable<string> enumNames)
            : base(fieldMeta)
        {
            EnumNames = enumNames;
            _defaultVal = defaultVal;
            Value = defaultVal ?? string.Empty;
            _DisplayName = this.WhenAnyValue(x => x.Value)
                .Select(x => x.ToString())
                .ToGuiProperty(this, nameof(DisplayName), string.Empty, deferSubscription: true);
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Value = property.GetString() ?? string.Empty;
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[Meta.DiskName] = JToken.FromObject(Value);
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumSettingsVM(Meta, _defaultVal, EnumNames);
        }

        public static EnumSettingsVM Factory(FieldMeta fieldMeta, object? defaultVal, Type enumType)
        {
            var names = Enum.GetNames(enumType).ToArray();
            return new EnumSettingsVM(fieldMeta, defaultVal?.ToString(), names);
        }
    }
}
