using Newtonsoft.Json.Linq;
using Noggog.WPF;
using Serilog;
using System;
using System.Text.Json;
using System.Threading;

namespace Synthesis.Bethesda.GUI
{
    public abstract class SettingsNodeVM : ViewModel
    {
        public string MemberName { get; }
        public Type TargetType { get; }

        public SettingsNodeVM(string memberName, Type targetType)
        {
            MemberName = memberName;
            TargetType = targetType;
        }

        public static SettingsNodeVM Factory(string memberName, Type targetType, object? defaultVal, CancellationToken cancel)
        {
            switch (targetType.Name)
            {
                case "Boolean":
                    return new BoolSettingsNodeVM(memberName, defaultVal);
                case "Int32":
                    return new IntSettingsNodeVM(memberName, defaultVal);
                case "Double":
                    return new DoubleSettingsNodeVM(memberName, defaultVal);
                default:
                    return new UnknownSettingsNodeVM(memberName);
            }
        }

        public abstract void Import(JsonProperty property, ILogger logger);

        public abstract void Persist(JObject obj, ILogger logger);
    }
}
