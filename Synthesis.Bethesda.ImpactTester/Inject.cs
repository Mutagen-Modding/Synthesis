using System;
using StructureMap;
using Synthesis.Bethesda.Execution.GitRespository;

namespace Synthesis.Bethesda.GUI
{
    public class Inject
    {
        public readonly static Container Instance;

        static Inject()
        {
            Instance = new Container(c =>
            {
                c.Scan(scanner =>
                {
                    scanner.AssemblyContainingType<IProvideRepositoryCheckouts>();
                    scanner.WithDefaultConventions();
                });
            });
#if DEBUG
            Instance.AssertConfigurationIsValid();
            Console.WriteLine(Instance.WhatDidIScan());
            Console.WriteLine(Instance.WhatDoIHave());
#endif
        }
    }
}