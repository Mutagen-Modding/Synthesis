using System.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface ICommandStringConstructor
    {
        string Get(string command, FilePath path, params string?[] args);
    }

    public class CommandStringConstructor : ICommandStringConstructor
    {
        public IStandardParameters Parameters { get; }

        public CommandStringConstructor(
            IStandardParameters parameters)
        {
            Parameters = parameters;
        }
        
        public string Get(string command, FilePath path, params string?[] args)
        {
            var argStr = string.Join(' ', args.NotNull());
            return $"{command} \"{path.RelativePath}\"{(Parameters.Parameters.IsNullOrWhitespace() ? string.Empty : $" {Parameters.Parameters}")}{(argStr.IsNullOrWhitespace() ? string.Empty : $" {argStr}")}";
        }
    }
}