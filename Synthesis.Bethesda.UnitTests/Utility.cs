using Mutagen.Bethesda;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Synthesis.Bethesda.UnitTests
{
    public static class Utility
    {
        public static readonly string OverallTempFolderPath = "SynthesisUnitTests";
        public static TempFolder GetTempFolder() => new TempFolder(Path.Combine(OverallTempFolderPath, Path.GetRandomFileName()));
        public static readonly string TypicalOutputFilename = "Synthesis.esp";
        public static string TypicalOutputFile(TempFolder tempFolder) => Path.Combine(tempFolder.Dir.Path, TypicalOutputFilename);
        public static readonly ModKey ModKey = new ModKey("Synthesis", ModType.Plugin);
    }
}
