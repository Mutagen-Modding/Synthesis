using Shouldly;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.ExecutablePath;

public class LiftExecutablePathTests
{
    [Theory, SynthAutoData]
    public void Success(LiftExecutablePath sut)
    {
        var lines = File.ReadAllLines(Utility.BuildSuccessFile);
        sut.TryGet(lines, out var path)
            .ShouldBeTrue();
        path.ShouldBe(@"C:\Repos\Patchers\khajiitearsshow\KhajiitEarsShow\bin\Debug\net6.0\KhajiitEarsShow.dll");
    }

    [Theory, SynthAutoData]
    public void SuccessNonEnglish(LiftExecutablePath sut)
    {
        var lines = File.ReadAllLines(Utility.BuildSuccessNonEnglishFile);
        sut.TryGet(lines, out var path)
            .ShouldBeTrue();
        path.ShouldBe(@"C:\Users\Andrew\AppData\Local\Temp\Synthesis\Loading\ugqvnbdg.i1q\bin\Debug\net6.0\win-x64\FaceFixer.dll");
    }

    [Theory, SynthAutoData]
    public void Failure(LiftExecutablePath sut)
    {
        var lines = File.ReadAllLines(Utility.BuildFailureFile);
        sut.TryGet(lines, out var _)
            .ShouldBeFalse();
    }

    [Theory, SynthInlineData("dll"), SynthInlineData("DLL")]
    public void SkipsLineWithoutDllExtension(
        string ext,
        LiftExecutablePath sut)
    {
        sut.TryGet(new string[]
            {
                $"Text {LiftExecutablePath.Delimiter} Path",
                $"Text {LiftExecutablePath.Delimiter} SomePath.{ext}"
            }, out var result)
            .ShouldBeTrue();
        result.ShouldBe($"SomePath.{ext}");
    }

    [Theory, SynthInlineData("dll"), SynthInlineData("DLL")]
    public void SkipsLineWithoutDelimiter(
        string ext,
        LiftExecutablePath sut)
    {
        sut.TryGet(new string[]
            {
                $"Text Path.{ext}",
                $"Text {LiftExecutablePath.Delimiter} SomePath.{ext}"
            }, out var result)
            .ShouldBeTrue();
        result.ShouldBe($"SomePath.{ext}");
    }

    [Theory, SynthInlineData("dll"), SynthInlineData("DLL")]
    public void LastLineLineWithDelimiter(
        string ext,
        LiftExecutablePath sut)
    {
        sut.TryGet(new string[]
            {
                $"Text {LiftExecutablePath.Delimiter} SomePath.{ext}",
                $"Text {LiftExecutablePath.Delimiter} SomePath2.{ext}"
            }, out var result)
            .ShouldBeTrue();
        result.ShouldBe($"SomePath2.{ext}");
    }
}