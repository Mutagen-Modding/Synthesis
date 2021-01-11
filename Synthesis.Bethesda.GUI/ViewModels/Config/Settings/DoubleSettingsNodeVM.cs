using Newtonsoft.Json.Linq;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DoubleSettingsNodeVM : SettingsNodeVM
    {
        [Reactive]
        public double Value { get; set; }

        public DoubleSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, typeof(bool))
        {
            if (defaultVal is double b)
            {
                Value = b;
            }
        }

        public override void Import(JsonProperty property, ILogger logger)
        {
            Value = property.Value.GetDouble();
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = Value;
        }
    }
}
