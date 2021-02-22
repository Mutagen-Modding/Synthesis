using DynamicData;
using Loqui;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json.Linq;
using Noggog;
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

        public static SettingsNodeVM[] Factory(SettingsParameters param, Type type, object? defaultObj)
        {
            return type.GetMembers()
                .Where(m => m.MemberType == MemberTypes.Property
                    || m.MemberType == MemberTypes.Field)
                .Where(m =>
                {
                    if (m is not PropertyInfo prop) return true;
                    return prop.GetSetMethod() != null;
                })
                .Where(m => m.GetCustomAttribute<SynthesisIgnoreSetting>() == null)
                .OrderBy(m =>
                {
                    if (m.TryGetCustomAttribute<SynthesisOrder>(out var order))
                    {
                        return order.Order;
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                })
                .Select(m =>
                {
                    try
                    {
                        return m switch
                        {
                            PropertyInfo prop => MemberFactory(param, prop, prop.PropertyType, defaultObj == null ? null : prop.GetValue(defaultObj)),
                            FieldInfo field => MemberFactory(param, field, field.FieldType, defaultObj == null ? null : field.GetValue(defaultObj)),
                            _ => throw new ArgumentException(),
                        };
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{type} failed to retrieve property {m}", ex);
                    }
                })
                .ToArray();
        }

        public static SettingsNodeVM[] Factory(SettingsParameters param, Type type)
        {
            return Factory(param, type, Activator.CreateInstance(type));
        }

        public virtual void WrapUp()
        {
        }

        public static SettingsNodeVM MemberFactory(SettingsParameters param, MemberInfo? member, Type targetType, object? defaultVal)
        {
            string memberName;
            if (member == null)
            {
                memberName = string.Empty;
            }
            else if (member.TryGetCustomAttribute<SynthesisSettingName>(out var nameAttr))
            {
                memberName = nameAttr.Name;
            }
            else
            {
                memberName = member.Name;
            }

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
                case "String":
                    return new StringSettingsVM(memberName, defaultVal);
                case "ModKey":
                    return new ModKeySettingsVM(param.DetectedLoadOrder.Transform(x => x.Listing.ModKey), memberName, defaultVal);
                case "FormKey":
                    return new FormKeySettingsVM(memberName, defaultVal);
                case "Array`1":
                case "List`1":
                case "IEnumerable`1":
                case "HashSet`1":
                    {
                        var firstGen = targetType.GenericTypeArguments[0];
                        switch (firstGen.Name)
                        {
                            case "SByte":
                                return EnumerableNumericSettingsVM.Factory<sbyte, Int8SettingsVM>(memberName, defaultVal, new Int8SettingsVM());
                            case "Int16":
                                return EnumerableNumericSettingsVM.Factory<short, Int16SettingsVM>(memberName, defaultVal, new Int16SettingsVM());
                            case "Int32":
                                return EnumerableNumericSettingsVM.Factory<int, Int32SettingsVM>(memberName, defaultVal, new Int32SettingsVM());
                            case "Int64":
                                return EnumerableNumericSettingsVM.Factory<long, Int64SettingsVM>(memberName, defaultVal, new Int64SettingsVM());
                            case "Byte":
                                return EnumerableNumericSettingsVM.Factory<byte, UInt8SettingsVM>(memberName, defaultVal, new UInt8SettingsVM());
                            case "UInt16":
                                return EnumerableNumericSettingsVM.Factory<ushort, UInt16SettingsVM>(memberName, defaultVal, new UInt16SettingsVM());
                            case "UInt32":
                                return EnumerableNumericSettingsVM.Factory<uint, UInt32SettingsVM>(memberName, defaultVal, new UInt32SettingsVM());
                            case "UInt64":
                                return EnumerableNumericSettingsVM.Factory<ulong, UInt64SettingsVM>(memberName, defaultVal, new UInt64SettingsVM());
                            case "Double":
                                return EnumerableNumericSettingsVM.Factory<double, DoubleSettingsVM>(memberName, defaultVal, new DoubleSettingsVM());
                            case "Single":
                                return EnumerableNumericSettingsVM.Factory<float, FloatSettingsVM>(memberName, defaultVal, new FloatSettingsVM());
                            case "Decimal":
                                return EnumerableNumericSettingsVM.Factory<decimal, DecimalSettingsVM>(memberName, defaultVal, new DecimalSettingsVM());
                            case "ModKey":
                                return EnumerableModKeySettingsVM.Factory(param, memberName, defaultVal);
                            case "FormKey":
                                return EnumerableFormKeySettingsVM.Factory(memberName, defaultVal);
                            case "String":
                                return EnumerableStringSettingsVM.Factory(memberName, defaultVal);
                            default:
                                {
                                    if (firstGen.Name.Contains("FormLink")
                                        && firstGen.IsGenericType
                                        && firstGen.GenericTypeArguments.Length == 1)
                                    {
                                        var formLinkGen = firstGen.GenericTypeArguments[0];
                                        return EnumerableFormLinkSettingsVM.Factory(param, memberName, formLinkGen.FullName ?? string.Empty, defaultVal);
                                    }
                                    var foundType = param.Assembly.GetType(firstGen.FullName!);
                                    if (foundType != null)
                                    {
                                        if (foundType.IsEnum)
                                        {
                                            return EnumerableEnumSettingsVM.Factory(memberName, defaultVal, foundType);
                                        }
                                        else
                                        {
                                            return EnumerableObjectSettingsVM.Factory(param, memberName, defaultVal, foundType);
                                        }
                                    }
                                    return new UnknownSettingsVM(memberName);
                                }
                        }
                    }
                case "Dictionary`2":
                    {
                        var firstGen = targetType.GenericTypeArguments[0];
                        if (member != null
                            && firstGen.IsEnum
                            && member.TryGetCustomAttribute<SynthesisStaticEnumDictionary>(out var _))
                        {
                            return EnumDictionarySettingsVM.Factory(param, memberName, firstGen, targetType.GenericTypeArguments[1], defaultVal);
                        }
                        else
                        {
                            return new UnknownSettingsVM(memberName);
                        }
                    }
                default:
                    {
                        if (targetType.Name.Contains("FormLink")
                            && targetType.IsGenericType
                            && targetType.GenericTypeArguments.Length == 1)
                        {
                            return new FormLinkSettingsVM(param.LinkCache, memberName, targetType);
                        }
                        var foundType = param.Assembly.GetType(targetType.FullName!);
                        if (foundType != null)
                        {
                            if (foundType.IsEnum)
                            {
                                return EnumSettingsVM.Factory(memberName, defaultVal, foundType);
                            }
                            else
                            {
                                return new ObjectSettingsVM(param, memberName, foundType, defaultVal);
                            }
                        }
                        return new UnknownSettingsVM(memberName);
                    }
            }
        }

        public abstract void Import(JsonElement property, ILogger logger);

        public abstract void Persist(JObject obj, ILogger logger);

        public abstract SettingsNodeVM Duplicate();
    }
}
