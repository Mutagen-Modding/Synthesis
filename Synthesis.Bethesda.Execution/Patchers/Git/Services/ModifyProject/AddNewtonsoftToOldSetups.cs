﻿using System.Xml.Linq;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;

public interface IAddNewtonsoftToOldSetups
{
    void Add(
        XElement proj,
        Version? mutaVersion,
        Version? synthVersion);
}

public class AddNewtonsoftToOldSetups : IAddNewtonsoftToOldSetups
{
    private readonly IProvideCurrentVersions _provideCurrentVersions;
    public static readonly System.Version NewtonSoftAddMutaVersion = new(0, 26);
    public static readonly System.Version NewtonSoftAddSynthVersion = new(0, 14, 1);

    public AddNewtonsoftToOldSetups(IProvideCurrentVersions provideCurrentVersions)
    {
        _provideCurrentVersions = provideCurrentVersions;
    }
        
    public void Add(
        XElement proj, 
        Version? mutaVersion,
        Version? synthVersion)
    {
        if ((mutaVersion != null
             && mutaVersion <= NewtonSoftAddMutaVersion)
            || (synthVersion != null
                && synthVersion <= NewtonSoftAddSynthVersion))
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var include)) continue;
                    if (include.Equals("Newtonsoft.Json")) return;
                }
            }

            proj.Add(new XElement("ItemGroup",
                new XElement("PackageReference",
                    new XAttribute("Include", "Newtonsoft.Json"),
                    new XAttribute("Version", _provideCurrentVersions.NewtonsoftVersion))));
        }
    }
}