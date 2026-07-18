using Noggog;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Settings.Calculators;

public class BuildCoreCalculator
{
    public const byte Mo2MaxCores = 2;

    private readonly IMo2EnvironmentDetector _mo2Detector;

    public BuildCoreCalculator(IMo2EnvironmentDetector mo2Detector)
    {
        _mo2Detector = mo2Detector;
    }

    public byte Calculate(double percent)
    {
        var target = Environment.ProcessorCount * Percent.FactoryPutInRange(percent);
        var ret = Math.Min(byte.MaxValue, target);
        ret = Math.Max(1, ret);
        if (_mo2Detector.IsRunningInsideMo2())
        {
            ret = Math.Min(ret, Mo2MaxCores);
        }
        return (byte)ret;
    }
}