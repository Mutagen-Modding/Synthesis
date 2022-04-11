using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Patchers.Git;

[ExcludeFromCodeCoverage]
public record CheckoutInput(
    string Proj,
    GitPatcherVersioning PatcherVersioning,
    NugetsVersioningTarget LibraryNugets);