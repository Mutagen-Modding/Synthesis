using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

[ExcludeFromCodeCoverage]
public record CheckoutInput(
    string Proj,
    GitPatcherVersioning PatcherVersioning,
    NugetsVersioningTarget LibraryNugets);