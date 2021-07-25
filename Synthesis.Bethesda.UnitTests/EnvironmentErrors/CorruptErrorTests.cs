using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Xml.Linq;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.EnvironmentErrors
{
    public class CorruptErrorTests
    {
        [Theory, SynthAutoData]
        public void CorruptFix(
            FilePath path,
            [Frozen]MockFileSystem fs,
            CorruptError sut)
        {
            fs.File.WriteAllText(path, "Whut");
            sut.RunFix(path);
            var doc = XDocument.Load(fs.FileStream.Create(path, FileMode.Open, FileAccess.Read));
            doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
        }
    }
}