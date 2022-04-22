using System.IO.Abstractions;
using System.Text.Json;
using Path = System.IO.Path;
using Newtonsoft.Json.Linq;
using Mutagen.Bethesda.WPF.Reflection;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Mutagen.Bethesda.Synthesis.WPF;

public class ReflectionSettingsVM : Mutagen.Bethesda.WPF.Reflection.ReflectionSettingsVM
{
    private readonly IPatcherExtraDataPathProvider _extraDataPathProvider;
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    public string SettingsFolder => _extraDataPathProvider.Path;
    public string SettingsSubPath { get; }
    public string SettingsPath => Path.Combine(_extraDataPathProvider.Path, SettingsSubPath);
    public string Nickname { get; }

    public delegate ReflectionSettingsVM Factory(
        ReflectionSettingsParameters param,
        string nickname,
        string settingsSubPath);

    public ReflectionSettingsVM(
        ReflectionSettingsParameters param,
        string nickname,
        IPatcherExtraDataPathProvider extraDataPathProvider,
        string settingsSubPath,
        ILogger logger,
        IFileSystem fileSystem)
        : base(param)
    {
        _extraDataPathProvider = extraDataPathProvider;
        _logger = logger;
        _fileSystem = fileSystem;
        Nickname = nickname;
        SettingsSubPath = settingsSubPath;
    }

    public async Task Import(
        CancellationToken cancel)
    {
        if (!_fileSystem.File.Exists(SettingsPath)) return;
        var txt = await _fileSystem.File.ReadAllTextAsync(SettingsPath, cancel).ConfigureAwait(false);
        var json = JsonDocument.Parse(txt, new JsonDocumentOptions()
        {
            AllowTrailingCommas = true
        });
        ObjVM.Import(json.RootElement, _logger.Information);
    }

    public void Persist()
    {
        _logger.Information($"Reflection settings folder: {_extraDataPathProvider.Path}");
        _logger.Information($"Reflection settings subpath: {SettingsSubPath}");
        var doc = new JObject();
        ObjVM.Persist(doc, _logger.Information);
        if (!_fileSystem.Directory.Exists(_extraDataPathProvider.Path))
        {
            _logger.Information($"Creating reflection settings directory");
            _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        }
        _logger.Information($"Writing reflection settings to: {SettingsPath}");
        _fileSystem.File.WriteAllText(SettingsPath, doc.ToString());
    }
}