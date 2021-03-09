using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mutagen.Bethesda.Synthesis
{
    public static class SynthesisWpfMixIn
    {
        public static SynthesisPipeline SetForWpf(
            this SynthesisPipeline pipe,
            SynthesisPipeline.OpenFunction? openForSettings,
            bool adjustArguments = true)
        {
            bool shutdown = true;
            pipe._onShutdown = (r) =>
            {
                System.Windows.Application.Current.Exit += (_, e) => e.ApplicationExitCode = r;
                if (shutdown)
                {
                    Application.Current.Shutdown(r);
                }
            };
            if (openForSettings != null)
            {
                pipe.SetOpenForSettings((r) =>
                {
                    shutdown = false;
                    openForSettings(r);
                });
            }
            if (adjustArguments)
            {
                // First argument is the path to the WPF app
                pipe.AdjustArguments(args => args.Skip(1).ToArray());
            }
            return pipe;
        }

        public static SynthesisPipeline SetForWpf<TWindow>(
            this SynthesisPipeline pipe, 
            bool adjustArguments = true)
            where TWindow : Window, new()
        {
            return SetForWpf(
                pipe: pipe,
                openForSettings: (r) =>
                {
                    var window = new TWindow();
                    window.Left = r.Left;
                    window.Top = r.Top;
                    window.ShowDialog();
                },
                adjustArguments: adjustArguments); 
        }
    }
}
