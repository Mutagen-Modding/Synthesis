using System.ComponentModel;
using System.Drawing;
using Mutagen.Bethesda.Synthesis.States;
using Synthesis.Bethesda.Commands;

namespace Mutagen.Bethesda.Synthesis;

public interface IOpenForSettingsState : IEnvironmentCreationState
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    OpenForSettings Settings { get; }
    
    Rectangle RecommendedOpenLocation { get; }
}