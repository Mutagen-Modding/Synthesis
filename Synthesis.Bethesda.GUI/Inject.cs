using System;
using Noggog.Reactive;
using Serilog;
using StructureMap;
using Synthesis.Bethesda.GUI.Registers;

namespace Synthesis.Bethesda.GUI
{
    public class Inject
    {
        public static Container Container { get; private set; } = null!;

        public Inject(Action<ConfigurationExpression> toAdd)
        {
            Container = new Container(c =>
            {
                c.IncludeRegistry<Register>();
                toAdd(c);
            });
        }
    }
}