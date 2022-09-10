using System.Windows;
using Mutagen.Bethesda.Synthesis.Internal;

namespace Mutagen.Bethesda.Synthesis;

public static class SynthesisWpfMixIn
{
    public static SynthesisPipeline SetForWpf(
        this SynthesisPipeline pipe, 
        bool stopShutdownForStandaloneRun = true,
        bool adjustArguments = true)
    {
        bool shutdown = true;
        pipe._onShutdown = (r) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Exit += (_, e) => e.ApplicationExitCode = r;
                if (shutdown)
                {
                    Application.Current.Shutdown(r);
                }
            });
        };
        pipe._runStyleCallback = (style) =>
        {
            switch (style)
            {
                case RunStyle.Standalone:
                    if (stopShutdownForStandaloneRun)
                    {
                        shutdown = false;
                    }
                    if (adjustArguments)
                    {
                        // First argument is the path to the WPF app
                        pipe.AdjustArguments(args => args.Skip(1).ToArray());
                    }
                    break;
                case RunStyle.OpenForSettings:
                    shutdown = false;
                    break;
                case RunStyle.QueryForSettings:
                    break;
                case RunStyle.RunPatcher:
                    break;
                case RunStyle.CheckRunnability:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, null);
            }
        };
        return pipe;
    }
}