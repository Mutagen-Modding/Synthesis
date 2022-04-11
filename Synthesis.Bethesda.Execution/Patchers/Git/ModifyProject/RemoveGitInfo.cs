using System.Linq;
using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;

public interface IRemoveGitInfo
{
    void Remove(XElement proj);
}

public class RemoveGitInfo : IRemoveGitInfo
{
    public void Remove(XElement proj)
    {
        foreach (var group in proj.Elements("ItemGroup"))
        {
            foreach (var elem in group.Elements("PackageReference").ToList())
            {
                if (elem.TryGetAttributeString("Include", out var includeAttr)
                    && includeAttr == "GitInfo")
                {
                    elem.Remove();
                    break;
                }
            }
        }
    }
}