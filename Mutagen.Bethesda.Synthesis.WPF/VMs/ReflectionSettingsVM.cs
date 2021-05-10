using System;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using Mutagen.Bethesda.WPF.Reflection;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class ReflectionSettingsVM : Mutagen.Bethesda.WPF.Reflection.ReflectionSettingsVM
    {
        public string SettingsFolder { get; }
        public string SettingsSubPath { get; }
        public string SettingsPath => Path.Combine(SettingsFolder, SettingsSubPath);
        public string Nickname { get; }

        public ReflectionSettingsVM(
            ReflectionSettingsParameters param,
            string nickname,
            string settingsFolder,
            string settingsSubPath)
            : base(param)
        {
            Nickname = nickname;
            SettingsFolder = settingsFolder;
            SettingsSubPath = settingsSubPath;
        }

        public async Task Import(
            Action<string> logger,
            CancellationToken cancel)
        {
            if (!File.Exists(SettingsPath)) return;
            var txt = await File.ReadAllTextAsync(SettingsPath, cancel);
            var json = JsonDocument.Parse(txt);
            ObjVM.Import(json.RootElement, logger);
        }

        public void Persist(Action<string> logger)
        {
            var doc = new JObject();
            ObjVM.Persist(doc, logger);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, doc.ToString());
        }
    }
}
