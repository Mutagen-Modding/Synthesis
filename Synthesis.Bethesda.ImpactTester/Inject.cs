using System;
using System.Collections.Generic;
using System.Linq;
using Noggog;
using SimpleInjector;
using Synthesis.Bethesda.Execution.GitRespository;

namespace Synthesis.Bethesda.GUI
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
                from type in typeof(IProvideRepositoryCheckouts).Assembly.GetExportedTypes()
                select type,
                Lifestyle.Singleton);
        }

        private void RegisterNamespaceFromType(Type type, Lifestyle lifestyle)
        {
            RegisterMatchingInterfaces(
                from t in type.Assembly.GetExportedTypes()
                where t.Namespace!.StartsWith(type.Namespace!)
                select t,
                lifestyle);
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
            type.GetInterfaces()
                .Where(i => IsMatchingInterface(i, type))
                .ForEach(i =>
                {
                    _coll.Register(i, type, lifestyle);
                });
        }

        private bool IsMatchingInterface(Type interf, Type concrete)
        {
            return interf.Name == $"I{concrete.Name}";
        }
    }
}