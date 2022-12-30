using System.IO.Abstractions;
using Mutagen.Bethesda.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.Internal;

public interface IReflectionSettingsTarget
{
    string? AnchorPath { get; set; }
    IFileSystem? FileSystem { get; set; }
}

public class ReflectionSettingsTarget<TSetting> : IReflectionSettingsTarget
    where TSetting : class, new()
{
    public IFileSystem? FileSystem { get; set; }
    private static readonly JsonSerializerSettings JsonSettings;

    public readonly Lazy<TSetting> Value;
    public string? AnchorPath { get; set; }
    public string SettingsPath { get; }
    public bool ThrowIfMissing { get; }

    static ReflectionSettingsTarget()
    {
        JsonSettings = new JsonSerializerSettings();
        JsonSettings.Converters.Add(new StringEnumConverter());
        JsonSettings.AddMutagenConverters();
        JsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
    }

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
        if (FileSystem.GetOrDefault().File.Exists(path))
        {
            System.Console.WriteLine($"Reading settings: {path}");
            var text = FileSystem.GetOrDefault().File.ReadAllText(path);
            var settings = JsonConvert.DeserializeObject<TSetting>(text, JsonSettings);
            if (settings == null)
            {
                throw new FileNotFoundException("Could not import the settings file to be an object", path);
            }
            return settings;
        }
        else
        {
            System.Console.WriteLine($"No settings file found.  Using defaults.  Path: {path}");
            if (ThrowIfMissing)
            {
                throw new FileNotFoundException("Cannot find required setting", path);
            }
            return new TSetting();
        }
    }
}