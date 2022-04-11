using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Xml.Linq;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.EnvironmentErrors;

public class NotExistsErrorTests
{
    [Theory, SynthAutoData]
    public void MissingFix(
        FilePath path,
        [Frozen]MockFileSystem fs,
        NotExistsError sut)
    {
        sut.RunFix(path);
        var doc = XDocument.Load(fs.FileStream.Create(path, FileMode.Open, FileAccess.Read));
        doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
    }
        
    [Theory, SynthAutoData]
    public void EmptyFileFix(
        FilePath path,
        [Frozen]MockFileSystem fs,
        CorruptError sut)
    {
        fs.File.WriteAllText(path, "");
        sut.RunFix(path);
        var doc = XDocument.Load(fs.FileStream.Create(path, FileMode.Open, FileAccess.Read));
        doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
    }

    [Theory, SynthAutoData]
    public void NoConfigurationFix(
        FilePath path,
        [Frozen]MockFileSystem fs,
        CorruptError sut)
    {
        fs.File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<something />");
        sut.RunFix(path);
        var doc = XDocument.Load(fs.FileStream.Create(path, FileMode.Open, FileAccess.Read));
        doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
    }
}