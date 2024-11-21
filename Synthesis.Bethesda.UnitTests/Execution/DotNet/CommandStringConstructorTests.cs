using FluentAssertions;
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
            .Should().Contain($"\"{path.RelativePath}\"");
    }
        
    [Theory, SynthAutoData]
    public void Typical(
        string command,
        FilePath path,
        string args,
        CommandStringConstructor sut)
    {
        sut.Get(command, path, args)
            .Should().Be(
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
            .Should().Be(
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
            .Should().Be(
                $"{command} \"{path}\" Hello World");
    }
        
    [Theory, SynthAutoData]
    public void NoArgs(
        string command,
        FilePath path,
        CommandStringConstructor sut)
    {
        sut.Get(command, path)
            .Should().Be(
                $"{command} \"{path}\"");
    }
}