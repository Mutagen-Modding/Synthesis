using Newtonsoft.Json.Linq;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class IntSettingsNodeVM : SettingsNodeVM
    {
        [Reactive]
        public int Value { get; set; }

        public IntSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, typeof(bool))
        {
            if (defaultVal is int b)
            {
                Value = b;
            }
        }

        public override void Import(JsonProperty property, ILogger logger)
        {
            Value = property.Value.GetInt32();
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = Value;
        }
    }
}
