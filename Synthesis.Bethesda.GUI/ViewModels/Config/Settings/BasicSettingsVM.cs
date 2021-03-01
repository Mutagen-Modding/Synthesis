using Newtonsoft.Json.Linq;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System.ComponentModel;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public interface IBasicSettingsNodeVM : INotifyPropertyChanged
    {
        string DisplayName { get; }

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

        public virtual string DisplayName => Value?.ToString() ?? "Name";

        public BasicSettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta)
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
            if (Value == null) return;
            obj[Meta.DiskName] = JToken.FromObject(Value);
        }

        public abstract T Get(JsonElement property);

        public abstract T GetDefault();
    }

    public class UnknownBasicSettingsVM : ViewModel, IBasicSettingsNodeVM
    {
        public static readonly UnknownBasicSettingsVM Empty = new UnknownBasicSettingsVM();

        public object Value => "Unknown";

        public bool IsSelected { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string DisplayName => "Unknown";

        public void WrapUp()
        {
            throw new System.NotImplementedException();
        }
    }
}
