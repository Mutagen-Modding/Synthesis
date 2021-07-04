using System;
using Noggog.Reactive;
using Serilog;
using StructureMap;

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
#if DEBUG
            var logging = Container.GetInstance<ILogger>();
            Container.AssertConfigurationIsValid();
            logging.Information(Container.WhatDidIScan());
            logging.Information(Container.WhatDoIHave());
#endif
        }
    }
}