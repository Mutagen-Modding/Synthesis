using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public record PotentialCheckoutInput(
    ConfigurationState RunnerState,
    GetResponse<string> Proj,
    GitPatcherVersioning PatcherVersioning,
    GetResponse<NugetsVersioningTarget> LibraryNugets);