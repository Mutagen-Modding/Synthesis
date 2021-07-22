using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public record CheckoutInput(
        ConfigurationState RunnerState,
        GetResponse<string> Proj,
        GitPatcherVersioning PatcherVersioning,
        GetResponse<NugetVersioningTarget> LibraryNugets);
}