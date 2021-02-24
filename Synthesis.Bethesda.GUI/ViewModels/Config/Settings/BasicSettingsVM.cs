using Newtonsoft.Json.Linq;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public interface IBasicSettingsNodeVM
    {
        object Value { get; }

        bool IsSelected { get; set; }
        void WrapUp();
    }

    public abstract class BasicSettingsVM<T> : SettingsNodeVM, IBasicSettingsNodeVM
    {
        public T DefaultValue { get; }

        [Reactive]
        public T Value { get; set; }

        object IBasicSettingsNodeVM.Value => this.Value!;

        [Reactive]
        public bool IsSelected { get; set; }

        public BasicSettingsVM(SettingsMeta memberName, object? defaultVal)
            : base(memberName)
        {
            if (defaultVal is T item)
            {
                Value = item;
                DefaultValue = item;
            }
            else
            {
                Value = default!;
                DefaultValue = default!;
            }
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Value = Get(property);
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[Meta.DiskName] = JToken.FromObject(Value!);
        }

        public abstract T Get(JsonElement property);

        public abstract T GetDefault();
    }
}
