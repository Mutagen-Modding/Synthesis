using LibGit2Sharp;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface ITag
    {
        string FriendlyName { get; }
        string Sha { get; }
    }

    public class TagWrapper : ITag
    {
        private readonly Tag _tag;

        public string FriendlyName => _tag.FriendlyName;
        public string Sha => _tag.Target.Sha;

        public TagWrapper(Tag tag)
        {
            _tag = tag;
        }
    }
}