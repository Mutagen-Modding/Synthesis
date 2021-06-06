namespace Synthesis.Bethesda.GUI.Profiles
{
    public interface IProfileProvider
    {
        ProfileVM Profile { get; }
    }
    
    public interface IProfileTracker : IProfileProvider
    {
        new ProfileVM Profile { get; set; }
    }

    public class ProfileTracker : IProfileTracker
    {
        public ProfileVM Profile { get; set; } = null!;
    }
}