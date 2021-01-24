using Newtonsoft.Json.Linq;
using Noggog.WPF;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
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

        public static SettingsNodeVM[] Factory(Assembly assemb, Type type)
        {
            var defaultObj = Activator.CreateInstance(type);
            return type.GetMembers()
                .Where(m => m.MemberType == MemberTypes.Property
                    || m.MemberType == MemberTypes.Field)
                .Select(m =>
                {
                    switch (m)
                    {
                        case PropertyInfo prop:
                            return MemberFactory(assemb, m.Name, prop.PropertyType, prop.GetValue(defaultObj));
                        case FieldInfo field:
                            return MemberFactory(assemb, m.Name, field.FieldType, field.GetValue(defaultObj));
                        default:
                            throw new ArgumentException();
                    }
                })
                .ToArray();
        }

        public static SettingsNodeVM MemberFactory(Assembly assemb, string memberName, Type targetType, object? defaultVal)
        {
            switch (targetType.Name)
            {
                case "Boolean":
                    return new BoolSettingsVM(memberName, defaultVal);
                case "SByte":
                    return new Int8SettingsVM(memberName, defaultVal);
                case "Int16":
                    return new Int16SettingsVM(memberName, defaultVal);
                case "Int32":
                    return new Int32SettingsVM(memberName, defaultVal);
                case "Int64":
                    return new Int64SettingsVM(memberName, defaultVal);
                case "Byte":
                    return new UInt8SettingsVM(memberName, defaultVal);
                case "UInt16":
                    return new UInt16SettingsVM(memberName, defaultVal);
                case "UInt32":
                    return new UInt32SettingsVM(memberName, defaultVal);
                case "UInt64":
                    return new UInt64SettingsVM(memberName, defaultVal);
                case "Double":
                    return new DoubleSettingsVM(memberName, defaultVal);
                case "Single":
                    return new FloatSettingsVM(memberName, defaultVal);
                case "Decimal":
                    return new DecimalSettingsVM(memberName, defaultVal);
                case "ModKey":
                    return new ModKeySettingsVM(memberName, defaultVal);
                case "Array`1":
                case "List`1":
                case "IEnumerable`1":
                    switch (targetType.GenericTypeArguments[0].Name)
                    {
                        case "SByte":
                            return EnumerableSettingsVM.Factory<sbyte, Int8SettingsVM>(memberName, defaultVal, new Int8SettingsVM());
                        case "Int16":
                            return EnumerableSettingsVM.Factory<short, Int16SettingsVM>(memberName, defaultVal, new Int16SettingsVM());
                        case "Int32":
                            return EnumerableSettingsVM.Factory<int, Int32SettingsVM>(memberName, defaultVal, new Int32SettingsVM());
                        case "Int64":
                            return EnumerableSettingsVM.Factory<long, Int64SettingsVM>(memberName, defaultVal, new Int64SettingsVM());
                        case "Byte":
                            return EnumerableSettingsVM.Factory<byte, UInt8SettingsVM>(memberName, defaultVal, new UInt8SettingsVM());
                        case "UInt16":
                            return EnumerableSettingsVM.Factory<ushort, UInt16SettingsVM>(memberName, defaultVal, new UInt16SettingsVM());
                        case "UInt32":
                            return EnumerableSettingsVM.Factory<uint, UInt32SettingsVM>(memberName, defaultVal, new UInt32SettingsVM());
                        case "UInt64":
                            return EnumerableSettingsVM.Factory<ulong, UInt64SettingsVM>(memberName, defaultVal, new UInt64SettingsVM());
                        case "Double":
                            return EnumerableSettingsVM.Factory<double, DoubleSettingsVM>(memberName, defaultVal, new DoubleSettingsVM());
                        case "Single":
                            return EnumerableSettingsVM.Factory<float, FloatSettingsVM>(memberName, defaultVal, new FloatSettingsVM());
                        case "Decimal":
                            return EnumerableSettingsVM.Factory<decimal, DecimalSettingsVM>(memberName, defaultVal, new DecimalSettingsVM());
                        default:
                            {
                                var foundType = assemb.GetType(targetType.GenericTypeArguments[0].FullName!);
                                if (foundType != null)
                                {
                                    return new EnumerableObjectSettingsVM(memberName, assemb, foundType);
                                }
                            }
                            return new UnknownSettingsVM(memberName);
                    }
                default:
                    {
                        var foundType = assemb.GetType(targetType.FullName!);
                        if (foundType != null)
                        {
                            return new ObjectSettingsVM(memberName, assemb, foundType);
                        }
                    }
                    return new UnknownSettingsVM(memberName);
            }
        }

        public abstract void Import(JsonElement property, ILogger logger);

        public abstract void Persist(JObject obj, ILogger logger);

        public abstract SettingsNodeVM Duplicate();
    }
}
