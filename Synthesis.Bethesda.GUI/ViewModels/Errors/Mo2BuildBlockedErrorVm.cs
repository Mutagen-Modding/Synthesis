using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for Mo2BuildBlockedErrorClassification.
/// This is shown when the BlockBuildingWithinMo2 setting actively blocked a build.
/// </summary>
public class Mo2BuildBlockedErrorVm : ErrorClassificationVm
{
    public delegate Mo2BuildBlockedErrorVm Factory(Mo2BuildBlockedErrorClassification error);

    public Mo2BuildBlockedErrorVm(
        Mo2BuildBlockedErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
    }
}
