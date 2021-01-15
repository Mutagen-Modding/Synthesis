using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using Serilog;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Synthesis.Bethesda.GUI
{
    public class ReflectionSettingsVM : ViewModel
    {
        private readonly Dictionary<string, SettingsNodeVM> _nodes;
        public string SettingsPath { get; }
        public string Nickname { get; }
        public ObservableCollection<SettingsNodeVM> Nodes { get; }

        public ReflectionSettingsVM(Type type, string nickname, string settingsPath)
        {
            Nickname = nickname;
            SettingsPath = settingsPath;
            var defaultObj = Activator.CreateInstance(type);
            _nodes = type.GetMembers()
                .Where(m => m.MemberType == MemberTypes.Property
                    || m.MemberType == MemberTypes.Field)
                .Select(m =>
                {
                    switch (m)
                    {
                        case PropertyInfo prop:
                            return SettingsNodeVM.Factory(m.Name, prop.PropertyType, prop.GetValue(defaultObj));
                        case FieldInfo field:
                            return SettingsNodeVM.Factory(m.Name, field.FieldType, field.GetValue(defaultObj));
                        default:
                            throw new ArgumentException();
                    }
                })
                .ToDictionary(x => x.MemberName);
            Nodes = new ObservableCollection<SettingsNodeVM>(_nodes.Values);
        }

        public async Task Import(
            ILogger logger,
            CancellationToken cancel)
        {
            if (!File.Exists(SettingsPath)) return;
            var txt = await File.ReadAllTextAsync(SettingsPath, cancel);
            var json = JsonDocument.Parse(txt);
            foreach (var elem in json.RootElement.EnumerateObject())
            {
                if (!_nodes.TryGetValue(elem.Name, out var node))
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

        public void Persist(ILogger logger)
        {
            var doc = new JObject();
            foreach (var node in _nodes.Values)
            {
                node.Persist(doc, logger);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, doc.ToString());
        }
    }
}
