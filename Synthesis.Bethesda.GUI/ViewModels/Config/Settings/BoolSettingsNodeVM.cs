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
    public class BoolSettingsNodeVM : SettingsNodeVM
    {
        [Reactive]
        public bool Value { get; set; }

        public BoolSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, typeof(bool))
        {
            if (defaultVal is bool b)
            {
                Value = b;
            }
        }

        public override void Import(JsonProperty property, ILogger logger)
        {
            Value = property.Value.GetBoolean();
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = Value;
        }
    }
}
