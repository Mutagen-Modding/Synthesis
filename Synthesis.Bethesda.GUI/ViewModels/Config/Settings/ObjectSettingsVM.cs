using Newtonsoft.Json.Linq;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using DynamicData;
using Noggog.WPF;
using DynamicData.Binding;
using Mutagen.Bethesda.Synthesis.Settings;
using System.Reflection;

namespace Synthesis.Bethesda.GUI
{
    public class ObjectSettingsVM : SettingsNodeVM
    {
        private readonly Dictionary<string, SettingsNodeVM> _nodes;
        public ObservableCollection<SettingsNodeVM> Nodes { get; }

        private readonly string[] _names;
        private Lazy<IObservableCollection<string>?> _nameTracker = null!;
        public IObservableCollection<string>? Names => _nameTracker.Value;

        public ObjectSettingsVM(FieldMeta fieldMeta, Dictionary<string, SettingsNodeVM> nodes, string[] names)
            : base(fieldMeta)
        {
            _nodes = nodes;
            _names = names;
            Nodes = new ObservableCollection<SettingsNodeVM>(_nodes.Values);
            Nodes.ForEach(n => n.Meta = n.Meta with
            {
                Parent = this,
                MainVM = this.Meta.MainVM
            });
            Init(_names);
        }

        public ObjectSettingsVM(SettingsParameters param, FieldMeta fieldMeta)
            : base(fieldMeta)
        {
            var nodes = Factory(param with { Parent = this, MainVM = this.Meta.MainVM });
            _nodes = nodes
                .ToDictionary(x => x.Meta.DiskName);
            _nodes.ForEach(n => n.Value.WrapUp());
            Nodes = new ObservableCollection<SettingsNodeVM>(_nodes.Values);
            _names = GetNamesToUse(param);
            Init(_names);
        }

        private void Init(string[] names)
        {
            if (names.Length > 0)
            {
                Meta = Meta with { IsPassthrough = false };
            }
            _nameTracker = new Lazy<IObservableCollection<string>?>(() =>
            {
                if (!names.Any()) return null;
                return names.AsObservableChangeSet()
                    .Transform(x =>
                    {
                        var setting = _nodes[x];
                        if (setting is IBasicSettingsNodeVM basic) return basic;
                        return UnknownBasicSettingsVM.Empty;
                    })
                    .AutoRefresh(x => x.DisplayName)
                    .Transform(n => n.DisplayName, transformOnRefresh: true)
                    .ToObservableCollection(this.CompositeDisposable);
            });
        }

        private static string[] GetNamesToUse(SettingsParameters param)
        {
            var attrs = param.TargetType.GetCustomAttributes(typeof(SynthesisObjectNameMember), inherit: false)
                .Select(x => (SynthesisObjectNameMember)x)
                .Select(x => x.Name)
                .ToArray();
            if (attrs.Length == 0) return Array.Empty<string>();
            var members = GetMemberInfos(param).ToArray();
            var selectedMembers = new List<MemberInfo>();
            foreach (var attr in attrs)
            {
                var memberInfo = members.FirstOrDefault(m => m.Name == attr);
                if (memberInfo != null)
                {
                    selectedMembers.Add(memberInfo);
                }
            }

            return selectedMembers
                .Select(m => GetDiskName(m))
                .ToArray();
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            ImportStatic(_nodes, property, logger);
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            PersistStatic(_nodes, Meta.DiskName, obj, logger);
        }

        public static void ImportStatic(
            Dictionary<string, SettingsNodeVM> nodes,
            JsonElement root,
            ILogger logger)
        {
            foreach (var elem in root.EnumerateObject())
            {
                if (!nodes.TryGetValue(elem.Name, out var node))
                {
                    logger.Error($"Could not locate proper node for setting with name: {elem.Name}");
                    continue;
                }
                try
                {
                    node.Import(elem.Value, logger);
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error(ex, $"Error parsing {elem.Name}");
                }
            }
        }

        public static void PersistStatic(
            Dictionary<string, SettingsNodeVM> nodes,
            string? name,
            JObject obj,
            ILogger logger)
        {
            if (!name.IsNullOrWhitespace())
            {
                var subObj = new JObject();
                obj[name] = subObj;
                obj = subObj;
            }
            foreach (var node in nodes.Values)
            {
                node.Persist(obj, logger);
            }
        }

        public override SettingsNodeVM Duplicate()
        {
            return new ObjectSettingsVM(
                Meta,
                this._nodes.Values
                    .Select(f =>
                    {
                        var ret = f.Duplicate();
                        ret.WrapUp();
                        return ret;
                    })
                    .ToDictionary(f => f.Meta.DiskName),
                _names);
        }
    }
}
