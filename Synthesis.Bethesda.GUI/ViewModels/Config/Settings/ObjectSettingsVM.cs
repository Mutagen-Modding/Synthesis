using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class ObjectSettingsVM : SettingsNodeVM, IReflectionObjectSettingsVM
    {
        private readonly Dictionary<string, SettingsNodeVM> _nodes;
        public ObservableCollection<SettingsNodeVM> Nodes { get; }

        public ObjectSettingsVM(string memberName, Assembly assemb, Type t)
            : base(memberName)
        {
            _nodes = Factory(assemb, t)
                .ToDictionary(x => x.MemberName);
            Nodes = new ObservableCollection<SettingsNodeVM>(_nodes.Values);
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            ImportStatic(_nodes, property, logger);
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            PersistStatic(_nodes, MemberName, obj, logger);
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
            if (name != null)
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
    }
}
