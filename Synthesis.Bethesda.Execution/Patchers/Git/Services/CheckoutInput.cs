using System.Diagnostics.CodeAnalysis;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

[ExcludeFromCodeCoverage]
public record CheckoutInput(
    string Proj,
    GitPatcherVersioning PatcherVersioning,
    NugetsVersioningTarget LibraryNugets);