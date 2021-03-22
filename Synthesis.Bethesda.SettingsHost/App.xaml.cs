using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Synthesis.Bethesda.SettingsHost
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var parse = Parser.Default.ParseArguments(e.Args, typeof(HostSettings));
            var result = parse
                .MapResult<HostSettings, int>(
                    (host) =>
                    {
                        DoHostSettings(host);
                        return 0;
                    },
                    _ => 
                    {
                        return -1;
                    });

            if (result == -1)
            {
                Application.Current.Shutdown(result);
            }
        }

        private void DoHostSettings(HostSettings host)
        {
            var window = new MainWindow();
            var vm = new MainVM(host);
            window.DataContext = vm;
            window.Closed += (a, b) =>
            {
                vm.Save();
            };
            window.Left = host.Left;
            window.Top = host.Top;
            window.Width = host.Width;
            window.Height = host.Height;
            window.Show();
        }
    }
}
