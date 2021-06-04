using Noggog;
using SimpleInjector;

namespace Synthesis.Bethesda.GUI
{
    public static class Inject
    {
        public readonly static Container Instance;

        static Inject()
        {
            var cont = new Container();
            Configure(cont);
            Instance = cont;
        }
        
        public static void Configure(Container coll)
        {
            coll.Register<MainVM>(Lifestyle.Singleton);
            coll.Register<IProvideInstalledSdk, ProvideInstalledSdk>(Lifestyle.Singleton);
            coll.Register<IEnvironmentErrorsVM, EnvironmentErrorsVM>(Lifestyle.Singleton);
            coll.Collection.Register<IEnvironmentErrorVM>(
                typeof(IEnvironmentErrorVM).Assembly.AsEnumerable(), 
                Lifestyle.Singleton);
        }
    }
}