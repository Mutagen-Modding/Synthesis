using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.IntegrationTests.Components;

/// <summary>
/// Test injection for IMo2EnvironmentDetector that allows controlling the result
/// </summary>
public class Mo2EnvironmentDetectorInjection : IMo2EnvironmentDetector
{
    public bool IsRunningInsideMo2Value { get; set; }

    public Mo2EnvironmentDetectorInjection(bool isRunningInsideMo2 = false)
    {
        IsRunningInsideMo2Value = isRunningInsideMo2;
    }

    public bool IsRunningInsideMo2() => IsRunningInsideMo2Value;
}
