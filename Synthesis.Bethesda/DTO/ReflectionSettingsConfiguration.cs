namespace Synthesis.Bethesda.DTO;

public record ReflectionSettingsConfig(string TypeName, string Nickname, string Path);
public record ReflectionSettingsConfigs(ReflectionSettingsConfig[] Configs);