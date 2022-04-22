using CommandLine;
using Noggog;

namespace Synthesis.Bethesda.GUI.Args;

public class ArgClassProvider
{
    private bool _retrieved;
    private object? _obj;
    
    public object? GetArgObject()
    {
        if (_retrieved) return _obj;

        _obj = ConstructArgObject();
        _retrieved = true;
        return _obj;
    }

    private object? ConstructArgObject()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        if (args.Length == 0) return null;

        return ConstructArgObject(args, first: true);
    }

    private object? ConstructArgObject(string[] args, bool first)
    {
        var parser = new Parser((s) =>
        {
            s.IgnoreUnknownArguments = true;
        });
        return parser.ParseArguments(
                args,
                typeof(StartCommand))
            .MapResult(
                (StartCommand start) =>
                {
                    return (object?)start;
                },
                _ =>
                {
                    if (!first)
                    {
                        return default(object?);
                    }

                    return ConstructArgObject("start".AsEnumerable().Concat(args).ToArray(), first: false);
                });
    }
}