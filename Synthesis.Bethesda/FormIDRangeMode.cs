namespace Synthesis.Bethesda;

public enum FormIDRangeMode
{
    Off,
    Auto,
    On,
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