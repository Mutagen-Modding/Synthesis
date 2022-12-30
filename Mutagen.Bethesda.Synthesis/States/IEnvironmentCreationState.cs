using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Records;

namespace Mutagen.Bethesda.Synthesis.States;

public interface IEnvironmentCreationState : IBaseRunState
{
    IGameEnvironment<TModSetter, TModGetter> GetEnvironmentState<TModSetter, TModGetter>()
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>;
}