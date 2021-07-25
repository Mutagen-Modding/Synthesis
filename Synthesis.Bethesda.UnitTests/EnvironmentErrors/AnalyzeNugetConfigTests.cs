using System.IO.Abstractions.TestingHelpers;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.EnvironmentErrors
{
    public class AnalyzeNugetConfigTests
    {
        [Theory, SynthAutoData]
        public void CorruptTrigger(
            FilePath path,
            [Frozen]MockFileSystem fs,
            AnalyzeNugetConfig sut)
        {
            fs.File.WriteAllText(path, "Whut");
            sut.Analyze(path)
                .Should().BeOfType<CorruptError>();
        }
        
        [Theory, SynthAutoData]
        public void MissingTrigger(
            FilePath path,
            AnalyzeNugetConfig sut)
        {
            sut.Analyze(path)
                .Should().BeOfType<NotExistsError>();
        }
        
        [Theory, SynthAutoData]
        public void EmptyFileTrigger(
            FilePath path,
            [Frozen]MockFileSystem fs,
            AnalyzeNugetConfig sut)
        {
            fs.File.WriteAllText(path, "");
            sut.Analyze(path)
                .Should().BeOfType<NotExistsError>();
        }
        
        [Theory, SynthAutoData]
        public void NoConfigurationTrigger(
            FilePath path,
            [Frozen]MockFileSystem fs,
            AnalyzeNugetConfig sut)
        {
            fs.File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<something />");
            sut.Analyze(path)
                .Should().BeOfType<NotExistsError>();
        }
        
        [Theory, SynthAutoData]
        public void EmptyPackageSourcesTrigger(
            FilePath path,
            [Frozen]MockFileSystem fs,
            AnalyzeNugetConfig sut)
        {
            fs.File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                       "<configuration>" +
                                       "<packageSources>" +
                                       "</packageSources>" +
                                       "</configuration>");
            sut.Analyze(path)
                .Should().BeOfType<MissingNugetOrgError>();
        }
        
        [Theory, SynthAutoData]
        public void OtherPackageSourcesTrigger(
            FilePath path,
            [Frozen]MockFileSystem fs,
            AnalyzeNugetConfig sut)
        {
            fs.File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                      "<configuration>" +
                                      "<packageSources>" +
                                      "<add key=\"CSharp Dev\" value=\"C:\\Repos\\CSharpExt\\Noggog.CSharpExt\\bin\\Debug\" />" +
                                      "</packageSources>" +
                                      "</configuration>");
            sut.Analyze(path)
                .Should().BeOfType<MissingNugetOrgError>();
        }
        
        [Theory, SynthAutoData]
        public void MissingPackageSourcesTrigger(
            FilePath path,
            [Frozen]MockFileSystem fs,
            AnalyzeNugetConfig sut)
        {
            fs.File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                       "<configuration>" +
                                       "</configuration>");
            sut.Analyze(path)
                .Should().BeOfType<MissingNugetOrgError>();
        }
    }
}