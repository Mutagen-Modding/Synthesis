using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet
{
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
            string standardArgs,
            CommandStringConstructor sut)
        {
            sut.Parameters.Parameters.Returns(standardArgs);
            sut.Get(command, path, args)
                .Should().Be(
                    $"{command} \"{path}\" {standardArgs} {args}");
        }
        
        [Theory, SynthAutoData]
        public void MultipleArgs(
            string command,
            FilePath path,
            string[] args,
            string standardArgs,
            CommandStringConstructor sut)
        {
            sut.Parameters.Parameters.Returns(standardArgs);
            sut.Get(command, path, args)
                .Should().Be(
                    $"{command} \"{path}\" {standardArgs} {string.Join(' ', args)}");
        }
        
        [Theory, SynthAutoData]
        public void TrimNullArgs(
            string command,
            FilePath path,
            string standardArgs,
            CommandStringConstructor sut)
        {
            sut.Parameters.Parameters.Returns(standardArgs);
            string?[] args = new string?[]
            {
                "Hello",
                null,
                "World"
            };
            sut.Get(command, path, args)
                .Should().Be(
                    $"{command} \"{path}\" {standardArgs} Hello World");
        }
        
        [Theory, SynthAutoData]
        public void NoArgs(
            string command,
            FilePath path,
            string standardArgs,
            CommandStringConstructor sut)
        {
            sut.Parameters.Parameters.Returns(standardArgs);
            sut.Get(command, path)
                .Should().Be(
                    $"{command} \"{path}\" {standardArgs}");
        }
        
        [Theory, SynthAutoData]
        public void JustArgs(
            string command,
            FilePath path,
            string args,
            CommandStringConstructor sut)
        {
            sut.Parameters.Parameters.Returns(string.Empty);
            sut.Get(command, path, args: args)
                .Should().Be(
                    $"{command} \"{path}\" {args}");
        }
        
        [Theory, SynthAutoData]
        public void Straight(
            string command,
            FilePath path,
            CommandStringConstructor sut)
        {
            sut.Parameters.Parameters.Returns(string.Empty);
            sut.Get(command, path)
                .Should().Be(
                    $"{command} \"{path}\"");
        }
    }
}