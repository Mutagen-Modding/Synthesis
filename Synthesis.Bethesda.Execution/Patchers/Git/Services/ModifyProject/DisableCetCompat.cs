using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;

/// <summary>
/// Mo2 does not play well with this turned on.  Seems unnecessary overall, so good to just have off
/// </summary>
public class TurnOffCetCompat
{
    public void TurnOff(XElement proj)
    {
        foreach (var group in proj.Elements("PropertyGroup"))
        {
            foreach (var elem in group.Elements())
            {
                if (elem.Name.LocalName.Equals("CETCompat"))
                {
                    if (elem.Value != "false")
                    {
                        elem.Value = "false";
                    }
                    return;
                }
            }
        }
        var propGroup = proj.Elements("PropertyGroup").FirstOrDefault();
        if (propGroup == null)
        {
            propGroup = new XElement("PropertyGroup");
            proj.Add(propGroup);
        }
        propGroup.Add(new XElement("CETCompat")
        {
            Value = "false"
        });
    }
}