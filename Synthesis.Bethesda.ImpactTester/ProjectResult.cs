using GitHubDependents;
using Noggog;

namespace Synthesis.Bethesda.ImpactTester;

record ProjectResult(
    Dependent Dependent,
    string SolutionFolderPath,
    string ProjSubPath,
    ErrorResponse Compile);