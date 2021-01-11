using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.Internal
{
    public interface IReflectionSettingsTarget
    {
        string? AnchorPath { get; set; }
    }

    public class ReflectionSettingsTarget<TSetting> : IReflectionSettingsTarget
        where TSetting : class, new()
    {
        public readonly Lazy<TSetting> Value;
        public string? AnchorPath { get; set; }
        public string SettingsPath { get; }
        public bool ThrowIfMissing { get; }

        public ReflectionSettingsTarget(
            string settingsPath,
            bool throwIfMissing)
        {
            SettingsPath = settingsPath;
            Value = new Lazy<TSetting>(Get);
            ThrowIfMissing = throwIfMissing;
        }

        private TSetting Get()
        {
            if (AnchorPath == null)
            {
                if (ThrowIfMissing)
                {
                    throw new FileNotFoundException("No extra data folder path specified");
                }
                return new TSetting();
            }
            var path = Path.Combine(AnchorPath, SettingsPath);
            System.Console.WriteLine($"Reading settings file: {path}");
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<TSetting>(File.ReadAllText(path));
            }
            else
            {
                if (ThrowIfMissing)
                {
                    throw new FileNotFoundException("Cannot find required setting", path);
                }
                return new TSetting();
            }
        }
    }
}
