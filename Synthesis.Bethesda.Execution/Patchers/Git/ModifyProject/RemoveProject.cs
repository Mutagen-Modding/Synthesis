using System.Linq;
using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject
{
    public interface IRemoveProject
    {
        void Remove(
            XElement proj,
            string packageName);
    }

    public class RemoveProject : IRemoveProject
    {
        public void Remove(
            XElement proj,
            string packageName)
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements().ToArray())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var libAttr)) continue;
                    if (!libAttr.Value.Equals(packageName)) continue;
                    elem.Remove();
                }
            }
        }
    }
}