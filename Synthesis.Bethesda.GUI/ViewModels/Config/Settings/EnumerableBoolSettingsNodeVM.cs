using Newtonsoft.Json.Linq;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableBoolSettingsNodeVM : SettingsNodeVM
    {
        [Reactive]
        public IEnumerable<bool> Value { get; set; } = Enumerable.Empty<bool>();

        public EnumerableBoolSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName)
        {
            if (defaultVal is IEnumerable<bool> b)
            {
                Value = b;
            }
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            int wer = 23;
            wer++;
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Value);
        }
    }
}
