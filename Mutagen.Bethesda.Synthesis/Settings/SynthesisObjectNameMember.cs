using Mutagen.Bethesda.WPF.Reflection.Attributes;

namespace Mutagen.Bethesda.Synthesis.Settings;

/// <summary>
/// Specifies a member to be displayed when the object is part of any summary areas, 
/// such as when scoping a child setting and this object is being displayed in the drill down summary
/// </summary>
public class SynthesisObjectNameMember : ObjectNameMember
{
    public SynthesisObjectNameMember(string name)
        : base(name)
    {
    }
}