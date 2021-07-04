using System;
using System.Linq;
using Noggog;
using StructureMap;
using StructureMap.Graph;
using StructureMap.Graph.Scanning;

namespace Synthesis.Bethesda.GUI
{
    public class SingletonConvention : IRegistrationConvention
    {
        public void ScanTypes(TypeSet types, Registry registry)
        {
            // Only work on concrete types
            types.FindTypes(TypeClassification.Concretes | TypeClassification.Closed).ForEach(type =>
            {
                // Register against all the interfaces implemented
                // by this concrete class
                Enumerable.Where<Type>(type.GetInterfaces(), i => i.Name == $"I{type.Name}")
                    .ForEach(i => registry.For(i).Use((Type) type).Singleton());
            });
        }
    }
}