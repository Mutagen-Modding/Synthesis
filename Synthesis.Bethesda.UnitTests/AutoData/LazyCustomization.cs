using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using Noggog;

namespace Synthesis.Bethesda.UnitTests.AutoData;

public class LazyBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type
            && IsLazyType(type))
        {
            return GetCreateMethod()
                .MakeGenericMethod(type.GenericTypeArguments[0])
                .Invoke(this, new object[] {context, request})!;
        }
        
        return new NoSpecimen();
    }

    public static bool IsLazyType(Type t)
    {
        return t.GenericTypeArguments.Length == 1
               && t.Name.StartsWith("Lazy");
    }

    public static MethodInfo GetCreateMethod()
    {
        return typeof(LazyBuilder)
            .GetMethod("CreateLazy", BindingFlags.Static | BindingFlags.Public)!;
    }

    public static Lazy<T> CreateLazy<T>(
        ISpecimenContext context,
        object request)
    {
        return new Lazy<T>(() => (T)context.Create<T>());
    }
}

public class LazyCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new LazyBuilder());
    }
}