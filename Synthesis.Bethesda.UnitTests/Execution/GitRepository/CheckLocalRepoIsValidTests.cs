﻿using FluentAssertions;
using Noggog.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository;

public class CheckLocalRepoIsValidTests
{
    [Theory, SynthAutoData]
    public void NullReturnsFalse(CheckLocalRepoIsValid sut)
    {
        sut.IsValidRepository(null)
            .Should().BeFalse();
    }
}