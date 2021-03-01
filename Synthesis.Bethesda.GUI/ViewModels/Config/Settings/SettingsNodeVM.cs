using DynamicData;
using Humanizer;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    [DebuggerDisplay("{Meta.DisplayName}")]
    public abstract class SettingsNodeVM : ViewModel
    {
        public FieldMeta Meta { get; set; }
        public Lazy<IEnumerable<SettingsNodeVM>> Parents { get; }

        public ICommand FocusSettingCommand { get; }

        private readonly ObservableAsPropertyHelper<bool>? _IsFocused;
        public bool IsFocused => _IsFocused?.Value ?? false;

        public SettingsNodeVM(FieldMeta fieldMeta)
        {
            Meta = fieldMeta;
            Parents = new Lazy<IEnumerable<SettingsNodeVM>>(() => GetParents().Reverse());
            FocusSettingCommand = ReactiveCommand.Create(() =>
            {
                var oldSetting = Meta.MainVM.SelectedSettings;
                Meta.MainVM.SelectedSettings = this;
                Meta.MainVM.ScrolledToSettings = oldSetting;
            });
            if (this.Meta.MainVM != null && !this.Meta.IsPassthrough)
            {
                _IsFocused = this.Meta.MainVM.WhenAnyValue(x => x.SelectedSettings)
                    .Select(x => IsFocusedCheck(x))
                    .ToGuiProperty(this, nameof(IsFocused), deferSubscription: true);
            }
        }

        public static IEnumerable<MemberInfo> GetMemberInfos(SettingsParameters param)
        {
            return param.TargetType.GetMembers()
                .Where(m => m.MemberType == MemberTypes.Property
                    || m.MemberType == MemberTypes.Field)
                .Where(m =>
                {
                    return m switch
                    {
                        PropertyInfo prop => !prop.IsStatic() && prop.GetSetMethod() != null,
                        FieldInfo field => !field.IsStatic && !field.IsInitOnly,
                        _ => true,
                    };
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
                });
        }

        public static SettingsNodeVM[] Factory(SettingsParameters param)
        {
            return GetMemberInfos(param)
                .Select(m =>
                {
                    try
                    {
                        var node = m switch
                        {
                            PropertyInfo prop => MemberFactory(param with
                            {
                                TargetType = prop.PropertyType,
                                DefaultVal = param.DefaultVal == null ? null : prop.GetValue(param.DefaultVal)
                            }, prop),
                            FieldInfo field => MemberFactory(param with
                            {
                                TargetType = field.FieldType,
                                DefaultVal = param.DefaultVal == null ? null : field.GetValue(param.DefaultVal)
                            }, field),
                            _ => throw new ArgumentException(),
                        };
                        return node;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{param.TargetType} failed to retrieve property {m}", ex);
                    }
                })
                .ToArray();
        }

        public virtual void WrapUp()
        {
        }

        public static string GetDiskName(MemberInfo? member)
        {
            string diskName;
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
            return diskName;
        }

        public static string GetDisplayName(MemberInfo? member)
        {
            string displayName;
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
                displayName = member.Name.Humanize(LetterCasing.Title);
            }
            return displayName;
        }

        public static SettingsNodeVM MemberFactory(SettingsParameters param, MemberInfo? member)
        {
            string displayName = GetDisplayName(member);
            string diskName = GetDiskName(member);

            string? tooltip = null;
            if (member != null && member.TryGetCustomAttribute<SynthesisTooltip>(out var toolTipAttr))
            {
                tooltip = toolTipAttr.Text;
            }

            var meta = new FieldMeta(
                DisplayName: displayName,
                DiskName: diskName,
                Tooltip: tooltip,
                MainVM: param.MainVM,
                Parent: param.Parent,
                IsPassthrough: false);

            switch (param.TargetType.Name)
            {
                case "Boolean":
                    return new BoolSettingsVM(meta, param.DefaultVal);
                case "SByte":
                    return new Int8SettingsVM(meta, param.DefaultVal);
                case "Int16":
                    return new Int16SettingsVM(meta, param.DefaultVal);
                case "Int32":
                    return new Int32SettingsVM(meta, param.DefaultVal);
                case "Int64":
                    return new Int64SettingsVM(meta, param.DefaultVal);
                case "Byte":
                    return new UInt8SettingsVM(meta, param.DefaultVal);
                case "UInt16":
                    return new UInt16SettingsVM(meta, param.DefaultVal);
                case "UInt32":
                    return new UInt32SettingsVM(meta, param.DefaultVal);
                case "UInt64":
                    return new UInt64SettingsVM(meta, param.DefaultVal);
                case "Double":
                    return new DoubleSettingsVM(meta, param.DefaultVal);
                case "Single":
                    return new FloatSettingsVM(meta, param.DefaultVal);
                case "Decimal":
                    return new DecimalSettingsVM(meta, param.DefaultVal);
                case "String":
                    return new StringSettingsVM(meta, param.DefaultVal);
                case "ModKey":
                    return new ModKeySettingsVM(param.DetectedLoadOrder.Transform(x => x.Listing.ModKey), meta, param.DefaultVal);
                case "FormKey":
                    return new FormKeySettingsVM(meta, param.DefaultVal);
                case "Array`1":
                case "List`1":
                case "IEnumerable`1":
                case "HashSet`1":
                    {
                        var firstGen = param.TargetType.GenericTypeArguments[0];
                        switch (firstGen.Name)
                        {
                            case "SByte":
                                return EnumerableNumericSettingsVM.Factory<sbyte, Int8SettingsVM>(meta, param.DefaultVal, new Int8SettingsVM());
                            case "Int16":
                                return EnumerableNumericSettingsVM.Factory<short, Int16SettingsVM>(meta, param.DefaultVal, new Int16SettingsVM());
                            case "Int32":
                                return EnumerableNumericSettingsVM.Factory<int, Int32SettingsVM>(meta, param.DefaultVal, new Int32SettingsVM());
                            case "Int64":
                                return EnumerableNumericSettingsVM.Factory<long, Int64SettingsVM>(meta, param.DefaultVal, new Int64SettingsVM());
                            case "Byte":
                                return EnumerableNumericSettingsVM.Factory<byte, UInt8SettingsVM>(meta, param.DefaultVal, new UInt8SettingsVM());
                            case "UInt16":
                                return EnumerableNumericSettingsVM.Factory<ushort, UInt16SettingsVM>(meta, param.DefaultVal, new UInt16SettingsVM());
                            case "UInt32":
                                return EnumerableNumericSettingsVM.Factory<uint, UInt32SettingsVM>(meta, param.DefaultVal, new UInt32SettingsVM());
                            case "UInt64":
                                return EnumerableNumericSettingsVM.Factory<ulong, UInt64SettingsVM>(meta, param.DefaultVal, new UInt64SettingsVM());
                            case "Double":
                                return EnumerableNumericSettingsVM.Factory<double, DoubleSettingsVM>(meta, param.DefaultVal, new DoubleSettingsVM());
                            case "Single":
                                return EnumerableNumericSettingsVM.Factory<float, FloatSettingsVM>(meta, param.DefaultVal, new FloatSettingsVM());
                            case "Decimal":
                                return EnumerableNumericSettingsVM.Factory<decimal, DecimalSettingsVM>(meta, param.DefaultVal, new DecimalSettingsVM());
                            case "ModKey":
                                return EnumerableModKeySettingsVM.Factory(param, meta, param.DefaultVal);
                            case "FormKey":
                                return EnumerableFormKeySettingsVM.Factory(meta, param.DefaultVal);
                            case "String":
                                return EnumerableStringSettingsVM.Factory(meta, param.DefaultVal);
                            default:
                                {
                                    if (firstGen.Name.Contains("FormLink")
                                        && firstGen.IsGenericType
                                        && firstGen.GenericTypeArguments.Length == 1)
                                    {
                                        var formLinkGen = firstGen.GenericTypeArguments[0];
                                        return EnumerableFormLinkSettingsVM.Factory(param, meta, formLinkGen.FullName ?? string.Empty, param.DefaultVal);
                                    }
                                    var foundType = param.Assembly.GetType(firstGen.FullName!);
                                    if (foundType != null)
                                    {
                                        if (foundType.IsEnum)
                                        {
                                            return EnumerableEnumSettingsVM.Factory(meta, param.DefaultVal, foundType);
                                        }
                                        else
                                        {
                                            return EnumerableObjectSettingsVM.Factory(param with { TargetType = foundType }, meta);
                                        }
                                    }
                                    return new UnknownSettingsVM(meta);
                                }
                        }
                    }
                case "Dictionary`2":
                    {
                        var firstGen = param.TargetType.GenericTypeArguments[0];
                        var secondGen = param.TargetType.GenericTypeArguments[1];
                        if (member != null
                            && firstGen.IsEnum
                            && (!member.TryGetCustomAttribute<SynthesisStaticEnumDictionary>(out var staticEnumAttr)
                            || staticEnumAttr.Enabled))
                        {
                            return EnumDictionarySettingsVM.Factory(param with { TargetType = secondGen }, meta, firstGen);
                        }
                        else if (firstGen == typeof(string))
                        {
                            return DictionarySettingsVM.Factory(param with
                            {
                                TargetType = secondGen
                            }, meta);
                        }
                        return new UnknownSettingsVM(meta);
                    }
                default:
                    {
                        if (param.TargetType.Name.Contains("FormLink")
                            && param.TargetType.IsGenericType
                            && param.TargetType.GenericTypeArguments.Length == 1)
                        {
                            return FormLinkSettingsVM.Factory(param.LinkCache, meta, param.TargetType, param.DefaultVal);
                        }
                        var foundType = param.Assembly.GetType(param.TargetType.FullName!);
                        if (foundType != null)
                        {
                            if (foundType.IsEnum)
                            {
                                return EnumSettingsVM.Factory(meta, param.DefaultVal, foundType);
                            }
                            else
                            {
                                return new ObjectSettingsVM(param with { TargetType = foundType }, meta);
                            }
                        }
                        return new UnknownSettingsVM(meta);
                    }
            }
        }

        public abstract void Import(JsonElement property, ILogger logger);

        public abstract void Persist(JObject obj, ILogger logger);

        public abstract SettingsNodeVM Duplicate();

        private IEnumerable<SettingsNodeVM> GetParents()
        {
            SettingsNodeVM? vm = this;
            while (vm.Meta.Parent != null)
            {
                if (!vm.Meta.Parent.Meta.IsPassthrough)
                {
                    yield return vm.Meta.Parent;
                }
                vm = vm.Meta.Parent;
            }
        }

        private bool IsFocusedCheck(SettingsNodeVM target)
        {
            SettingsNodeVM? vm = this;
            do
            {
                if (target == vm) return true;
                if (!vm.Meta.Parent?.Meta.IsPassthrough ?? true) return false;
                vm = vm.Meta.Parent;
            }
            while (vm != null);
            return false;
        }
    }
}
