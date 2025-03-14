﻿using Shouldly;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution;

public class ProjectPathConstructorTests
{
    [Theory, SynthAutoData]
    public void CombinesSolutionDirWithSubPath(
        ProjectPathConstructor sut)
    {
        sut.Construct("C:/SolutionDir/SolutionPath.sln", "SubPath")
            .ShouldBe(new FilePath("C:/SolutionDir/SubPath"));
    }
        
    [Theory, SynthAutoData]
    public void ErrorReturnsDefault(
        ProjectPathConstructor sut)
    {
        sut.Construct("C:/SolutionDir/SolutionPath.sln", null!)
            .ShouldBe(new FilePath());
    }
}