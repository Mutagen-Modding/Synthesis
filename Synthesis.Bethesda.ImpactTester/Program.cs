using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.ImpactTester;

class Program
{
    static async Task Main(string[] args)
    {
        CancellationTokenSource cancel = new();
        await Task.WhenAny(
            Task.Run(async () =>
            {
                try
                {
                    var builder = new ContainerBuilder();
                    builder.RegisterModule<MainModule>();
                    builder.RegisterType<InlineWorkDropoff>().As<IWorkDropoff>();
                    var tester = builder.Build().Resolve<Tester>();
                    if (args.Length == 2)
                    {
                        await tester.DoWork(
                            new NugetVersionPair(
                                args[0],
                                args[1]),
                            cancel.Token);
                    }
                    else
                    {
                        await tester.DoWork(
                            new NugetVersionPair(null, null),
                            cancel.Token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }),
            Task.Run(() =>
            {
                System.Console.ReadLine();
                cancel.Cancel();
            }));
    }
}