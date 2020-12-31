using Mutagen.Bethesda;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Synthesis.Bethesda.UnitTests
{
    public static class Utility
    {
        public static readonly string OverallTempFolderPath = "SynthesisUnitTests";
        public static TempFolder GetTempFolder() => new TempFolder(Path.Combine(OverallTempFolderPath, Path.GetRandomFileName()))
        {
             ThrowIfUnsuccessfulDisposal = false
        };
        public static readonly string TypicalOutputFilename = "Synthesis.esp";
        public static readonly ModKey TestModKey = new ModKey("test", ModType.Plugin);
        public static readonly ModKey OverrideModKey = new ModKey("override", ModType.Plugin);
        public static readonly string TestFileName = "test.esp";
        public static readonly string OverrideFileName = "override.esp";
        public static readonly string OtherFileName = "other.esp";
        public static readonly string Other2FileName = "other2.esp";
        public static readonly string OblivionPathToTestFile = "oblivion_test.esp";
        public static readonly string OblivionPathToOverrideFile = "oblivion_override.esp";
        public static readonly string LePathToTestFile = "le_test.esp";
        public static readonly string LePathToOverrideFile = "le_override.esp";
        public static readonly string PathToLoadOrderFile = "Plugins.txt";
        public static readonly ModKey RandomModKey = new ModKey("Random", ModType.Plugin);

        public static ModPath TypicalOutputFile(TempFolder tempFolder) => Path.Combine(tempFolder.Dir.Path, TypicalOutputFilename);
        public static IEnumerable<LoadOrderListing> TypicalLoadOrder(GameRelease release, DirectoryPath dir) => PluginListings.ListingsFromPath(PathToLoadOrderFile, release, dir);

        public static TempFolder SetupDataFolder(TempFolder tempFolder, GameRelease release, string? loadOrderPath = null)
        {
            var dataFolder = new TempFolder(Path.Combine(tempFolder.Dir.Path, "Data"));
            dataFolder.Dir.DeleteEntireFolder();
            dataFolder.Dir.Create();
            loadOrderPath ??= PathToLoadOrderFile;
            string testPath, overridePath;
            switch (release)
            {
                case GameRelease.Oblivion:
                    testPath = OblivionPathToTestFile;
                    overridePath = OblivionPathToOverrideFile;
                    break;
                case GameRelease.SkyrimLE:
                case GameRelease.SkyrimSE:
                    testPath = LePathToTestFile;
                    overridePath = LePathToOverrideFile;
                    break;
                default:
                    throw new NotImplementedException();
            }
            File.Copy(testPath, Path.Combine(dataFolder.Dir.Path, TestFileName));
            File.Copy(overridePath, Path.Combine(dataFolder.Dir.Path, OverrideFileName));
            var loadOrderListing = PluginListings.ListingsFromPath(loadOrderPath, release, dataFolder.Dir);
            LoadOrder.AlignTimestamps(loadOrderListing.OnlyEnabled().Select(m => m.ModKey), dataFolder.Dir.Path);
            return dataFolder;
        }
    }
}
