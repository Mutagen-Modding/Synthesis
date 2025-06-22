using System.IO.Abstractions;
using AutoFixture.Xunit2;
using Shouldly;
using Noggog;
using Noggog.Testing.AutoFixture;
using Noggog.Testing.Extensions;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution;

public class AvailableProjectsRetrieverTests
{
    [Theory, SynthAutoData(FileSystem: TargetFileSystem.Substitute)]
    public void PathDoesNotExistReturnsEmpty(
        [Frozen]IFileSystem fs,
        FilePath solutionPath,
        AvailableProjectsRetriever sut)
    {
        fs.File.Exists(default).ReturnsForAnyArgs(false);
        sut.Get(solutionPath)
            .ShouldBeEmpty();
    }
    
    [Theory, SynthAutoData]
    public void TypicalSolution(
        [Frozen]IFileSystem fs,
        FilePath filePath,
        AvailableProjectsRetriever sut)
    {
        fs.File.WriteAllText(filePath,  @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.30330.147
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""RaceCompatibilityDialogue"", ""RaceCompatibilityDialogue\RaceCompatibilityDialogue.csproj"", ""{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Tests"", ""Tests\Tests.csproj"", ""{8F99ACBF-612A-422D-952C-E1239AFC1F2F}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Solution Items"", ""Solution Items"", ""{DD760590-5E36-4473-BBFD-A0CBE48F4310}""
	ProjectSection(SolutionItems) = preProject
		.releaserc = .releaserc
		.github\workflows\ci-dev.yaml = .github\workflows\ci-dev.yaml
		.github\workflows\ci-prod.yaml = .github\workflows\ci-prod.yaml
		coverlet.runsettings = coverlet.runsettings
		README.md = README.md
		renovate.json = renovate.json
	EndProjectSection
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Release|Any CPU.Build.0 = Release|Any CPU
		{8F99ACBF-612A-422D-952C-E1239AFC1F2F}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{8F99ACBF-612A-422D-952C-E1239AFC1F2F}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{8F99ACBF-612A-422D-952C-E1239AFC1F2F}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{8F99ACBF-612A-422D-952C-E1239AFC1F2F}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {4F8D594C-B197-4CBB-831B-A80B4F180599}
	EndGlobalSection
EndGlobal
");
        sut.Get(filePath)
	        .Select(x => x.TrimStart(filePath.Directory!, StringComparison.InvariantCulture))
            .ShouldEqualEnumerable(
		        "RaceCompatibilityDialogue\\RaceCompatibilityDialogue.csproj",
		        "Tests\\Tests.csproj");
    }
}