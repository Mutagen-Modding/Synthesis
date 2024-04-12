namespace Synthesis.Bethesda;

public enum FormIDRangeMode
{
    Auto = 1,
    Off = 2,
    On = 3,
}

public static class FormIDRangeModeExt
{
    public static bool? ToForceBool(this FormIDRangeMode mode)
    {
        return mode switch
        {
            FormIDRangeMode.Auto => null,
            FormIDRangeMode.Off => false,
            FormIDRangeMode.On => true,
            _ => throw new NotImplementedException(),
        };
    }
}