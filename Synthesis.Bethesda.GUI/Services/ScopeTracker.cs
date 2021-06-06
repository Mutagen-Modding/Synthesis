using SimpleInjector;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IScopeProvider<out T>
    {
        T Item { get; }
        Scope Scope { get; }
    }
    
    public interface IScopeTracker<T> : IScopeProvider<T>
    {
        new T Item { get; set; }
        new Scope Scope { get; set; }
    }

    public class ScopeTracker<T> : IScopeTracker<T>
    {
        public T Item { get; set; } = default!;
        public Scope Scope { get; set; } = null!;
    }
}