using Autofac;

namespace Synthesis.Bethesda.Execution.Modules;

public enum RegistrationStyle
{
    Singleton,
    Transient
}

public static class ContainerBuilderExtensions
{
    /// <summary>
    /// Registers every type in the namespace of <typeparamref name="TPrototype"/> with the given lifetime.
    /// Use a folder-per-lifetime convention (e.g. Services/Singletons) and anchor on any type living in it.
    /// </summary>
    public static void RegisterFolder<TPrototype>(this ContainerBuilder builder, RegistrationStyle style)
    {
        var registration = builder.RegisterAssemblyTypes(typeof(TPrototype).Assembly)
            .InNamespaceOf<TPrototype>()
            .AsImplementedInterfaces()
            .AsSelf();

        if (style == RegistrationStyle.Singleton)
        {
            registration.SingleInstance();
        }
    }
}
