using System.IO;
using FluentAssertions;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.ExecutablePath
{
    public class LiftExecutablePathTests
    {
        [Theory, SynthAutoData]
        public void Success(LiftExecutablePath sut)
        {
            var lines = File.ReadAllLines(Utility.BuildSuccessFile);
            sut.TryGet(lines, out var path)
                .Should().BeTrue();
            path.Should().Be(@"C:\Repos\Patchers\khajiitearsshow\KhajiitEarsShow\bin\Debug\net5.0\KhajiitEarsShow.dll");
        }

        [Theory, SynthAutoData]
        public void SuccessNonEnglish(LiftExecutablePath sut)
        {
            var lines = File.ReadAllLines(Utility.BuildSuccessNonEnglishFile);
            sut.TryGet(lines, out var path)
                .Should().BeTrue();
            path.Should().Be(@"C:\Users\Andrew\AppData\Local\Temp\Synthesis\Loading\ugqvnbdg.i1q\bin\Debug\net5.0\win-x64\FaceFixer.dll");
        }

        [Theory, SynthAutoData]
        public void Failure(LiftExecutablePath sut)
        {
            var lines = File.ReadAllLines(Utility.BuildFailureFile);
            sut.TryGet(lines, out var _)
                .Should().BeFalse();
        }
    }
}