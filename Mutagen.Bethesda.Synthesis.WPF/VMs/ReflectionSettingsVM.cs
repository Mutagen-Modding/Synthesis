using System;
using System.IO.Abstractions;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using Path = System.IO.Path;
using Newtonsoft.Json.Linq;
using Mutagen.Bethesda.WPF.Reflection;
using Serilog;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class ReflectionSettingsVM : Mutagen.Bethesda.WPF.Reflection.ReflectionSettingsVM
    {
        private readonly ILogger _Logger;
        private readonly IFileSystem _FileSystem;
        public string SettingsFolder { get; }
        public string SettingsSubPath { get; }
        public string SettingsPath => Path.Combine(SettingsFolder, SettingsSubPath);
        public string Nickname { get; }

        public ReflectionSettingsVM(
            ReflectionSettingsParameters param,
            string nickname,
            string settingsFolder,
            string settingsSubPath,
            ILogger logger,
            IFileSystem fileSystem)
            : base(param)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
            Nickname = nickname;
            SettingsFolder = settingsFolder;
            SettingsSubPath = settingsSubPath;
        }

        public async Task Import(
            CancellationToken cancel)
        {
            if (!_FileSystem.File.Exists(SettingsPath)) return;
            var txt = await _FileSystem.File.ReadAllTextAsync(SettingsPath, cancel);
            var json = JsonDocument.Parse(txt, new JsonDocumentOptions()
            {
                AllowTrailingCommas = true
            });
            ObjVM.Import(json.RootElement, _Logger.Information);
        }

        public void Persist()
        {
            _Logger.Information($"Reflection settings folder: {SettingsFolder}");
            _Logger.Information($"Reflection settings subpath: {SettingsSubPath}");
            var doc = new JObject();
            ObjVM.Persist(doc, _Logger.Information);
            if (!_FileSystem.Directory.Exists(SettingsFolder))
            {
                _Logger.Information($"Creating reflection settings directory");
                _FileSystem.Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            }
            _Logger.Information($"Writing reflection settings to: {SettingsPath}");
            _FileSystem.File.WriteAllText(SettingsPath, doc.ToString());
        }
    }
}
