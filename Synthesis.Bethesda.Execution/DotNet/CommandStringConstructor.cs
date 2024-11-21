using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface ICommandStringConstructor
{
    string Get(string command, FilePath path, params string?[] args);
}

public class CommandStringConstructor : ICommandStringConstructor
{
    public string Get(string command, FilePath path, params string?[] args)
    {
        var argStr = string.Join(' ', args.WhereNotNull());
        return $"{command} \"{path.RelativePath}\"{(argStr.IsNullOrWhitespace() ? string.Empty : $" {argStr}")}";
    }
}