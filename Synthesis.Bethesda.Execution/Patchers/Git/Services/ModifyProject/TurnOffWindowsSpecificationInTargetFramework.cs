﻿using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;

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
                    && elem.Value.EndsWith("-windows7.0", StringComparison.Ordinal))
                {
                    elem.Value = Noggog.StringExt.TrimStringFromEnd(elem.Value, "-windows7.0");
                }
            }
        }
    }
}