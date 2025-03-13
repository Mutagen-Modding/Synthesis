using Shouldly;
using Noggog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet;

public class CommandStringConstructorTests
{
    [Theory, SynthAutoData]
    public void PutsInQuotes(
        string command,
        FilePath path,
        string args,
        CommandStringConstructor sut)
    {
        sut.Get(command, path, args)
            .ShouldContain($"\"{path.RelativePath}\"");
    }
        
    [Theory, SynthAutoData]
    public void Typical(
        string command,
        FilePath path,
        string args,
        CommandStringConstructor sut)
    {
        sut.Get(command, path, args)
            .ShouldBe(
                $"{command} \"{path}\" {args}");
    }
        
    [Theory, SynthAutoData]
    public void MultipleArgs(
        string command,
        FilePath path,
        string[] args,
        CommandStringConstructor sut)
    {
        sut.Get(command, path, args)
            .ShouldBe(
                $"{command} \"{path}\" {string.Join(' ', args)}");
    }
        
    [Theory, SynthAutoData]
    public void TrimNullArgs(
        string command,
        FilePath path,
        CommandStringConstructor sut)
    {
        string?[] args = new string?[]
        {
            "Hello",
            null,
            "World"
        };
        sut.Get(command, path, args)
            .ShouldBe(
                $"{command} \"{path}\" Hello World");
    }
        
    [Theory, SynthAutoData]
    public void NoArgs(
        string command,
        FilePath path,
        CommandStringConstructor sut)
    {
        sut.Get(command, path)
            .ShouldBe(
                $"{command} \"{path}\"");
    }
}