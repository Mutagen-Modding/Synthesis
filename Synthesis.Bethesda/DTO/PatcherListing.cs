using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.DTO;

public class PatcherListing
{
    public PatcherCustomization? Customization { get; set; }
    public string ProjectPath { get; set; } = string.Empty;
}