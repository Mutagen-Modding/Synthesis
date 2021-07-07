using System;
using StructureMap;
using Synthesis.Bethesda.Execution.CLI;

namespace Synthesis.Bethesda.CLI
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
                    scanner.AssemblyContainingType<ICheckRunnability>();
                    scanner.WithDefaultConventions();
                });
            });
        }
    }
}