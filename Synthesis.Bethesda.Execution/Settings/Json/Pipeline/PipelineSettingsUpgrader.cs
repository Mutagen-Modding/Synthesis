using System;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V1;
using Vers1 = Synthesis.Bethesda.Execution.Settings.V1.PipelineSettings;
using Vers2 = Synthesis.Bethesda.Execution.Settings.V2.PipelineSettings;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline;

public interface IPipelineSettingsUpgrader
{
    Vers2 Upgrade(object o);
}

public class PipelineSettingsUpgrader : IPipelineSettingsUpgrader
{
    private readonly IPipelineSettingsV1Upgrader _v1;

    public PipelineSettingsUpgrader(
        IPipelineSettingsV1Upgrader v1)
    {
        _v1 = v1;
    }
        
    public Vers2 Upgrade(object o)
    {
        switch (o)
        {
            case Vers1 ver1:
                return _v1.Upgrade(ver1);
            case Vers2 ver2:
                return ver2;
            default:
                throw new NotImplementedException();
        }
    }
}