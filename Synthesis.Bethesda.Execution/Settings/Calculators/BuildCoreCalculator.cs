﻿using Noggog;

namespace Synthesis.Bethesda.Execution.Settings.Calculators;

public class BuildCoreCalculator
{
    public byte Calculate(double percent)
    {
        return 1;
        var target = Environment.ProcessorCount * Percent.FactoryPutInRange(percent);
        var ret = Math.Min(byte.MaxValue, target);
        ret = Math.Max(1, ret);
        return (byte)ret;
    }
}