using DynamicData;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json.Linq;
using Noggog;
using Noggog.WPF;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public abstract class SettingsNodeVM : ViewModel
    {
        public SettingsMeta Meta { get; set; }

        public SettingsNodeVM(SettingsMeta memberName)
        {
            Meta = memberName;
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
            string displayName, diskName;
            if (member == null)
            {
                displayName = string.Empty;
            }
            else if (member.TryGetCustomAttribute<SynthesisSettingName>(out var nameAttr))
            {
                displayName = nameAttr.Name;
            }
            else
            {
                displayName = member.Name;
            }
            if (member == null)
            {
                diskName = string.Empty;
            }
            else if (member.TryGetCustomAttribute<SynthesisDiskName>(out var diskAttr))
            {
                diskName = diskAttr.Name;
            }
            else
            {
                diskName = member.Name;
            }

            string? tooltip = null;
            if (member != null && member.TryGetCustomAttribute<SynthesisTooltip>(out var toolTipAttr))
            {
                tooltip = toolTipAttr.Text;
            }

            var meta = new SettingsMeta(
                DisplayName: displayName, 
                DiskName: diskName,
                Tooltip: tooltip);

            switch (targetType.Name)
            {
                case "Boolean":
                    return new BoolSettingsVM(meta, defaultVal);
                case "SByte":
                    return new Int8SettingsVM(meta, defaultVal);
                case "Int16":
                    return new Int16SettingsVM(meta, defaultVal);
                case "Int32":
                    return new Int32SettingsVM(meta, defaultVal);
                case "Int64":
                    return new Int64SettingsVM(meta, defaultVal);
                case "Byte":
                    return new UInt8SettingsVM(meta, defaultVal);
                case "UInt16":
                    return new UInt16SettingsVM(meta, defaultVal);
                case "UInt32":
                    return new UInt32SettingsVM(meta, defaultVal);
                case "UInt64":
                    return new UInt64SettingsVM(meta, defaultVal);
                case "Double":
                    return new DoubleSettingsVM(meta, defaultVal);
                case "Single":
                    return new FloatSettingsVM(meta, defaultVal);
                case "Decimal":
                    return new DecimalSettingsVM(meta, defaultVal);
                case "String":
                    return new StringSettingsVM(meta, defaultVal);
                case "ModKey":
                    return new ModKeySettingsVM(param.DetectedLoadOrder.Transform(x => x.Listing.ModKey), meta, defaultVal);
                case "FormKey":
                    return new FormKeySettingsVM(meta, defaultVal);
                case "Array`1":
                case "List`1":
                case "IEnumerable`1":
                case "HashSet`1":
                    {
                        var firstGen = targetType.GenericTypeArguments[0];
                        switch (firstGen.Name)
                        {
                            case "SByte":
                                return EnumerableNumericSettingsVM.Factory<sbyte, Int8SettingsVM>(meta, defaultVal, new Int8SettingsVM());
                            case "Int16":
                                return EnumerableNumericSettingsVM.Factory<short, Int16SettingsVM>(meta, defaultVal, new Int16SettingsVM());
                            case "Int32":
                                return EnumerableNumericSettingsVM.Factory<int, Int32SettingsVM>(meta, defaultVal, new Int32SettingsVM());
                            case "Int64":
                                return EnumerableNumericSettingsVM.Factory<long, Int64SettingsVM>(meta, defaultVal, new Int64SettingsVM());
                            case "Byte":
                                return EnumerableNumericSettingsVM.Factory<byte, UInt8SettingsVM>(meta, defaultVal, new UInt8SettingsVM());
                            case "UInt16":
                                return EnumerableNumericSettingsVM.Factory<ushort, UInt16SettingsVM>(meta, defaultVal, new UInt16SettingsVM());
                            case "UInt32":
                                return EnumerableNumericSettingsVM.Factory<uint, UInt32SettingsVM>(meta, defaultVal, new UInt32SettingsVM());
                            case "UInt64":
                                return EnumerableNumericSettingsVM.Factory<ulong, UInt64SettingsVM>(meta, defaultVal, new UInt64SettingsVM());
                            case "Double":
                                return EnumerableNumericSettingsVM.Factory<double, DoubleSettingsVM>(meta, defaultVal, new DoubleSettingsVM());
                            case "Single":
                                return EnumerableNumericSettingsVM.Factory<float, FloatSettingsVM>(meta, defaultVal, new FloatSettingsVM());
                            case "Decimal":
                                return EnumerableNumericSettingsVM.Factory<decimal, DecimalSettingsVM>(meta, defaultVal, new DecimalSettingsVM());
                            case "ModKey":
                                return EnumerableModKeySettingsVM.Factory(param, meta, defaultVal);
                            case "FormKey":
                                return EnumerableFormKeySettingsVM.Factory(meta, defaultVal);
                            case "String":
                                return EnumerableStringSettingsVM.Factory(meta, defaultVal);
                            default:
                                {
                                    if (firstGen.Name.Contains("FormLink")
                                        && firstGen.IsGenericType
                                        && firstGen.GenericTypeArguments.Length == 1)
                                    {
                                        var formLinkGen = firstGen.GenericTypeArguments[0];
                                        return EnumerableFormLinkSettingsVM.Factory(param, meta, formLinkGen.FullName ?? string.Empty, defaultVal);
                                    }
                                    var foundType = param.Assembly.GetType(firstGen.FullName!);
                                    if (foundType != null)
                                    {
                                        if (foundType.IsEnum)
                                        {
                                            return EnumerableEnumSettingsVM.Factory(meta, defaultVal, foundType);
                                        }
                                        else
                                        {
                                            return EnumerableObjectSettingsVM.Factory(param, meta, defaultVal, foundType);
                                        }
                                    }
                                    return new UnknownSettingsVM(meta);
                                }
                        }
                    }
                case "Dictionary`2":
                    {
                        var firstGen = targetType.GenericTypeArguments[0];
                        var secondGen = targetType.GenericTypeArguments[1];
                        if (member != null
                            && firstGen.IsEnum
                            && member.TryGetCustomAttribute<SynthesisStaticEnumDictionary>(out var staticEnumAttr)
                            && staticEnumAttr.Enabled)
                        {
                            return EnumDictionarySettingsVM.Factory(param, meta, firstGen, secondGen, defaultVal);
                        }
                        else if (firstGen == typeof(string))
                        {
                            return DictionarySettingsVM.Factory(param, meta, valType: secondGen, defaultVal: defaultVal);
                        }
                        return new UnknownSettingsVM(meta);
                    }
                default:
                    {
                        if (targetType.Name.Contains("FormLink")
                            && targetType.IsGenericType
                            && targetType.GenericTypeArguments.Length == 1)
                        {
                            return FormLinkSettingsVM.Factory(param.LinkCache, meta, targetType, defaultVal);
                        }
                        var foundType = param.Assembly.GetType(targetType.FullName!);
                        if (foundType != null)
                        {
                            if (foundType.IsEnum)
                            {
                                return EnumSettingsVM.Factory(meta, defaultVal, foundType);
                            }
                            else
                            {
                                return new ObjectSettingsVM(param, meta, foundType, defaultVal);
                            }
                        }
                        return new UnknownSettingsVM(meta);
                    }
            }
        }

        public abstract void Import(JsonElement property, ILogger logger);

        public abstract void Persist(JObject obj, ILogger logger);

        public abstract SettingsNodeVM Duplicate();
    }
}
