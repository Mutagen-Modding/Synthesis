using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface IDotNetCommandPathProvider
{
    string Path { get; }
}

[ExcludeFromCodeCoverage]
public class DotNetCommandPathProvider : IDotNetCommandPathProvider
{
    private readonly IDotNetPathSettingsProvider _settings;
    public string Path => _settings.DotNetPathOverride.IsNullOrWhitespace() ? "dotnet" : _settings.DotNetPathOverride;

    public DotNetCommandPathProvider(IDotNetPathSettingsProvider settings)
    {
        _settings = settings;
    }
}