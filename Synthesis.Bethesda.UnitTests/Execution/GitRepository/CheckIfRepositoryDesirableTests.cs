﻿using Shouldly;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository;

public class CheckIfRepositoryDesirableTests
{
    [Theory, SynthAutoData]
    public void RepositoryUndesirableIfNoMainBranch(
        IGitRepository repo,
        CheckIfRepositoryDesirable sut)
    {
        repo.MainBranch.Returns(default(IBranch?));
        sut.IsDesirable(repo)
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void RepositoryDesirableOtherwise(
        IGitRepository repo,
        CheckIfRepositoryDesirable sut)
    {
        sut.IsDesirable(repo)
            .Succeeded.ShouldBeTrue();
    }
}