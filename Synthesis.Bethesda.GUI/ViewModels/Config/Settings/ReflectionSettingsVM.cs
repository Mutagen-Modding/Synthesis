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
using LibGit2Sharp;

namespace Synthesis.Bethesda.GUI
{
    public interface IReflectionObjectSettingsVM
    {
        ObservableCollection<SettingsNodeVM> Nodes { get; }
    }

    public class ReflectionSettingsVM : ViewModel, IReflectionObjectSettingsVM
    {
        private readonly Dictionary<string, SettingsNodeVM> _nodes;
        public string SettingsFolder { get; }
        public string SettingsSubPath { get; }
        public string SettingsPath => Path.Combine(SettingsFolder, SettingsSubPath);
        public string Nickname { get; }
        public ObservableCollection<SettingsNodeVM> Nodes { get; }

        public ReflectionSettingsVM(
            SettingsParameters param,
            Type type, 
            string nickname, 
            string settingsFolder,
            string settingsSubPath)
        {
            Nickname = nickname;
            SettingsFolder = settingsFolder;
            SettingsSubPath = settingsSubPath;
            _nodes = SettingsNodeVM.Factory(param, type)
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
            ObjectSettingsVM.ImportStatic(_nodes, json.RootElement, logger);
        }

        public void Persist(ILogger logger)
        {
            var doc = new JObject();
            ObjectSettingsVM.PersistStatic(_nodes, null, doc, logger);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, doc.ToString());
            if (!Repository.IsValid(SettingsFolder))
            {
                Repository.Init(SettingsFolder);
            }
            using var repo = new Repository(SettingsFolder);
            Commands.Stage(repo, SettingsSubPath);
            var sig = new Signature("Synthesis", "someEmail@gmail.com", DateTimeOffset.Now);
            try
            {
                repo.Commit("Settings changed", sig, sig);
            }
            catch (EmptyCommitException)
            {
            }
        }
    }
}
