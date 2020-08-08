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
        public static readonly ModKey ModKey = new ModKey("Synthesis", ModType.Plugin);
        public static readonly string PathToTestFile = "../../../test.esp";
        public static readonly ModKey TestModKey = new ModKey("test", ModType.Plugin);
        public static readonly string PathToOverrideFile = "../../../override.esp";
        public static readonly ModKey OverrideModKey = new ModKey("override", ModType.Plugin);
        public static readonly string PathToLoadOrderFile = "../../../Plugins.txt";

        public static string TypicalOutputFile(TempFolder tempFolder) => Path.Combine(tempFolder.Dir.Path, TypicalOutputFilename);
        public static IEnumerable<ModKey> TypicalLoadOrder() => LoadOrder.FromPath(PathToLoadOrderFile);

        public static TempFolder SetupDataFolder(string? loadOrderPath = null)
        {
            var dataFolder = new TempFolder();
            loadOrderPath ??= PathToLoadOrderFile;
            File.Copy(Utility.PathToTestFile, Path.Combine(dataFolder.Dir.Path, Path.GetFileName(Utility.PathToTestFile)));
            File.Copy(Utility.PathToOverrideFile, Path.Combine(dataFolder.Dir.Path, Path.GetFileName(Utility.PathToOverrideFile)));
            var loadOrderListing = LoadOrder.FromPath(loadOrderPath);
            LoadOrder.AlignTimestamps(loadOrderListing, dataFolder.Dir.Path);
            return dataFolder;
        }
    }
}
