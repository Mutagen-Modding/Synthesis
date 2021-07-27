using System;
using System.Linq;
using System.Xml.Linq;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject
{
    public interface ITurnOffNullability
    {
        void TurnOff(XElement proj);
    }

    public class TurnOffNullability : ITurnOffNullability
    {
        public void TurnOff(XElement proj)
        {
            foreach (var group in proj.Elements("PropertyGroup"))
            {
                foreach (var elem in @group.Elements())
                {
                    if (elem.Name.LocalName.Equals("WarningsAsErrors"))
                    {
                        var warnings = elem.Value.Split(',');
                        elem.Value = string.Join(',', warnings.Where(x => !x.Contains("nullable", StringComparison.OrdinalIgnoreCase)));
                    }
                }
            }
        }
    }
}