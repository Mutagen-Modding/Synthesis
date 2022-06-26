namespace Mutagen.Bethesda.Synthesis.Settings;

[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false)]
public class SynthesisDescription : Attribute
{
    public string Text { get; }

    public SynthesisDescription(string text)
    {
        Text = text;
    }
}