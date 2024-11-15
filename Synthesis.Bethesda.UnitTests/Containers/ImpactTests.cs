﻿using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.ImpactTester;

namespace Synthesis.Bethesda.UnitTests.Containers;

public class ImpactTests
{
    [Fact]
    public void ImpactModule()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<Synthesis.Bethesda.ImpactTester.MainModule>();
        var cont = builder.Build();
        cont.Validate(typeof(Tester));
    }
}