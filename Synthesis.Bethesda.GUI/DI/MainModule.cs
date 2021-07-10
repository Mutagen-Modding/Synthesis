using Autofac;

namespace Synthesis.Bethesda.GUI.DI
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<TopLevelModule>();
            builder.RegisterModule<ProfileModule>();
        }
    }
}