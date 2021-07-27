using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject
{
    public interface ITurnOffWindowsSpecificationInTargetFramework
    {
        void TurnOff(XElement proj);
    }

    public class TurnOffWindowsSpecificationInTargetFramework : ITurnOffWindowsSpecificationInTargetFramework
    {
        public void TurnOff(XElement proj)
        {
            foreach (var group in proj.Elements("PropertyGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (elem.Name.LocalName.Equals("TargetFramework")
                        && elem.Value.EndsWith("-windows7.0"))
                    {
                        elem.Value = elem.Value.TrimEnd("-windows7.0");
                    }
                }
            }
        }
    }
}