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

        public SettingsNodeVM(string memberName)
        {
            MemberName = memberName;
        }

        public static SettingsNodeVM Factory(string memberName, Type targetType, object? defaultVal)
        {
            switch (targetType.Name)
            {
                case "Boolean":
                    return new BoolSettingsNodeVM(memberName, defaultVal);
                case "SByte":
                    return new Int8SettingsNodeVM(memberName, defaultVal);
                case "Int16":
                    return new Int16SettingsNodeVM(memberName, defaultVal);
                case "Int32":
                    return new Int32SettingsNodeVM(memberName, defaultVal);
                case "Int64":
                    return new Int64SettingsNodeVM(memberName, defaultVal);
                case "Byte":
                    return new UInt8SettingsNodeVM(memberName, defaultVal);
                case "UInt16":
                    return new UInt16SettingsNodeVM(memberName, defaultVal);
                case "UInt32":
                    return new UInt32SettingsNodeVM(memberName, defaultVal);
                case "UInt64":
                    return new UInt64SettingsNodeVM(memberName, defaultVal);
                case "Double":
                    return new DoubleSettingsNodeVM(memberName, defaultVal);
                case "Single":
                    return new FloatSettingsNodeVM(memberName, defaultVal);
                case "Decimal":
                    return new DecimalSettingsNodeVM(memberName, defaultVal);
                case "Array`1":
                case "List`1":
                case "IEnumerable`1":
                case "HashSet`1":
                    switch (targetType.GenericTypeArguments[0].Name)
                    {
                        case "SByte":
                            return EnumerableSettingsNodeVM.Factory<sbyte, Int8SettingsNodeVM>(memberName, defaultVal, new Int8SettingsNodeVM());
                        case "Int16":
                            return EnumerableSettingsNodeVM.Factory<short, Int16SettingsNodeVM>(memberName, defaultVal, new Int16SettingsNodeVM());
                        case "Int32":
                            return EnumerableSettingsNodeVM.Factory<int, Int32SettingsNodeVM>(memberName, defaultVal, new Int32SettingsNodeVM());
                        case "Int64":
                            return EnumerableSettingsNodeVM.Factory<long, Int64SettingsNodeVM>(memberName, defaultVal, new Int64SettingsNodeVM());
                        case "Byte":
                            return EnumerableSettingsNodeVM.Factory<byte, UInt8SettingsNodeVM>(memberName, defaultVal, new UInt8SettingsNodeVM());
                        case "UInt16":
                            return EnumerableSettingsNodeVM.Factory<ushort, UInt16SettingsNodeVM>(memberName, defaultVal, new UInt16SettingsNodeVM());
                        case "UInt32":
                            return EnumerableSettingsNodeVM.Factory<uint, UInt32SettingsNodeVM>(memberName, defaultVal, new UInt32SettingsNodeVM());
                        case "UInt64":
                            return EnumerableSettingsNodeVM.Factory<ulong, UInt64SettingsNodeVM>(memberName, defaultVal, new UInt64SettingsNodeVM());
                        case "Double":
                            return EnumerableSettingsNodeVM.Factory<double, DoubleSettingsNodeVM>(memberName, defaultVal, new DoubleSettingsNodeVM());
                        case "Single":
                            return EnumerableSettingsNodeVM.Factory<float, FloatSettingsNodeVM>(memberName, defaultVal, new FloatSettingsNodeVM());
                        case "Decimal":
                            return EnumerableSettingsNodeVM.Factory<decimal, DecimalSettingsNodeVM>(memberName, defaultVal, new DecimalSettingsNodeVM());
                        default:
                            return new UnknownSettingsNodeVM(memberName);
                    }
                default:
                    return new UnknownSettingsNodeVM(memberName);
            }
        }

        public abstract void Import(JsonElement property, ILogger logger);

        public abstract void Persist(JObject obj, ILogger logger);
    }
}
