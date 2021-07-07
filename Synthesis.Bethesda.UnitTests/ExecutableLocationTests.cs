using FluentAssertions;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.DotNet;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class ExecutableLocationTests
    {
        [Fact]
        public void Success()
        {
            var lines = File.ReadAllLines(Utility.BuildSuccessFile);
            DotNetCommands.TryGetExecutablePathFromOutput(lines, out var path)
                .Should().BeTrue();
            path.Should().Be(@"C:\Repos\Patchers\khajiitearsshow\KhajiitEarsShow\bin\Debug\net5.0\KhajiitEarsShow.dll");
        }

        [Fact]
        public void SuccessNonEnglish()
        {
            var lines = File.ReadAllLines(Utility.BuildSuccessNonEnglishFile);
            DotNetCommands.TryGetExecutablePathFromOutput(lines, out var path)
                .Should().BeTrue();
            path.Should().Be(@"C:\Users\Andrew\AppData\Local\Temp\Synthesis\Loading\ugqvnbdg.i1q\bin\Debug\net5.0\win-x64\FaceFixer.dll");
        }

        [Fact]
        public void Failure()
        {
            var lines = File.ReadAllLines(Utility.BuildFailureFile);
            DotNetCommands.TryGetExecutablePathFromOutput(lines, out var _)
                .Should().BeFalse();
        }
    }
}
