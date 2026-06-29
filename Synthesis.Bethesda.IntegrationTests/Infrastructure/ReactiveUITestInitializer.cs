using System.Runtime.CompilerServices;
using ReactiveUI.Builder;

namespace Synthesis.Bethesda.IntegrationTests.Infrastructure;

internal static class ReactiveUITestInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // ReactiveUI 23+ requires explicit initialization via the builder pattern
        // before any reactive members (e.g. WhenAnyValue) are used.
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .WithWpf()
            .BuildApp();
    }
}
