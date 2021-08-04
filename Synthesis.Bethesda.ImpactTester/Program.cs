using GitHubDependents;
using LibGit2Sharp;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Mutagen.Bethesda.Synthesis.Versioning;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.ImpactTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CancellationTokenSource cancel = new();
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    var builder = new ContainerBuilder();
                    builder.RegisterModule<MainModule>();
                    builder.RegisterType<Tester>().AsSelf();
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
                }),
                Task.Run(() =>
                {
                    System.Console.ReadLine();
                    cancel.Cancel();
                }));
        }
    }
}
