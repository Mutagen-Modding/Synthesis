using CommandLine;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.CLI;
using Synthesis.Bethesda.Commands;

namespace Synthesis.Bethesda.Execution.Utility;

public interface IFormatCommandLine
{
    string Format<T>(T obj);
}

public class FormatCommandLine : IFormatCommandLine
{
    public string Format<T>(T obj)
    {
        var ret = Parser.Default.FormatCommandLine<T>(obj);
        
        if (!ret.Contains("GameRelease")
            && obj is RunSynthesisMutagenPatcher or RunSynthesisPatcher or CheckRunnability)
        {
            // Hardcoded fix for GameRelease not being included
            // Can be removed if this gets fixed
            // https://github.com/commandlineparser/commandline/issues/850
            ret += $" --GameRelease {default(GameRelease)}";
        }

        return ret;
    }
}