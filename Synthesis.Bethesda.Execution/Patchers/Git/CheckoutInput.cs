namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public record CheckoutInput(
        string Proj,
        GitPatcherVersioning PatcherVersioning,
        NugetVersioningTarget LibraryNugets);
}