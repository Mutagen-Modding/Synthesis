using System;
using System.Collections.Generic;
using System.Linq;
using SimpleInjector;
using Synthesis.Bethesda.Execution.CLI;

namespace Synthesis.Bethesda.CLI
{
    public class Inject
    {
        private Container _coll = new();
        public readonly static Container Instance;

        static Inject()
        {
            var inject = new Inject();
            inject.Configure();
            Instance = inject._coll;
        }
        
        private void Configure()
        {
            RegisterMatchingInterfaces(
                from type in typeof(ICheckRunnability).Assembly.GetExportedTypes()
                where type.Namespace!.StartsWith("Synthesis.Bethesda.Execution.CLI")
                select type,
                Lifestyle.Singleton);
        }

        private void RegisterMatchingInterfaces(IEnumerable<Type> types, Lifestyle lifestyle)
        {
            foreach (var type in types)
            {
                RegisterMatchingInterfaces(type, lifestyle);
            }
        }

        private void RegisterMatchingInterfaces(Type type, Lifestyle lifestyle)
        {
            foreach (var i in type.GetInterfaces()
                .Where(i => IsMatchingInterface(i, type)))
            {
                _coll.Register(i, type, lifestyle);
            }
        }

        private bool IsMatchingInterface(Type interf, Type concrete)
        {
            return interf.Name == $"I{concrete.Name}";
        }
    }
}