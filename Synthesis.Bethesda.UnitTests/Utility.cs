using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.Utility;
using System.Collections;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.CompilerServices;
using NSubstitute;
using NSubstitute.Core;

namespace Synthesis.Bethesda.UnitTests;

public static class Utility
{
    public static readonly string OverallTempFolderPath = "SynthesisUnitTests";
    public static readonly ModKey SynthesisModKey = new("Synthesis", ModType.Plugin);
    public static readonly ModKey TestModKey = new("test", ModType.Plugin);
    public static readonly ModKey OverrideModKey = new("override", ModType.Plugin);
    public static readonly string TestFileName = "test.esp";
    public static readonly string OverrideFileName = "override.esp";
    public static readonly string OtherFileName = "other.esp";
    public static readonly string Other2FileName = "other2.esp";
    public static readonly string OblivionPathToTestFile = "Files/oblivion_test.esp";
    public static readonly string OblivionPathToOverrideFile = "Files/oblivion_override.esp";
    public static readonly string LePathToTestFile = "Files/le_test.esp";
    public static readonly string LePathToOverrideFile = "Files/le_override.esp";
    public static readonly FilePath PluginPath = "Files/Plugins.txt";
    public static readonly string BuildFailureFile = "Files/BuildFailure.txt";
    public static readonly string BuildSuccessFile = "Files/BuildSuccess.txt";
    public static readonly string BuildSuccessNonEnglishFile = "Files/BuildSuccessNonEnglish.txt";
    public static readonly ModKey RandomModKey = new("Random", ModType.Plugin);

    public static TempFolder GetTempFolder(string folderName, [CallerMemberName] string? testName = null)
    {
        return TempFolder.FactoryByAddedPath(Path.Combine(Utility.OverallTempFolderPath, folderName, testName!), throwIfUnsuccessfulDisposal: false);
    }
        
    public static ModPath TypicalOutputFile(DirectoryPath tempFolder) => Path.Combine(tempFolder.Path, SynthesisModKey.FileName);

    public const string BaseFolder = "C:/BaseFolder";

    public static TestEnvironment SetupEnvironment(GameRelease release)
    {
        var baseFolder = new DirectoryPath(BaseFolder);
        var dataFolder = new DirectoryPath($"{BaseFolder}/DataFolder");
        var pluginPath = Path.Combine(BaseFolder, PluginPath);
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

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { Path.Combine(dataFolder.Path, TestFileName), new MockFileData(File.ReadAllBytes(testPath)) },
            { Path.Combine(dataFolder.Path, OverrideFileName), new MockFileData(File.ReadAllBytes(overridePath)) },
            { pluginPath, new MockFileData(File.ReadAllBytes(PluginPath)) },
        });

        return new TestEnvironment(
            fileSystem,
            Release: release,
            DataFolder: dataFolder,
            BaseFolder: baseFolder,
            PluginPath: pluginPath);
    }
        
    public enum Return { True, False, Throw }
        
    public static ConfiguredCall Returns(
        this bool value,
        Return ret)
    {
        return value.Returns(_ =>
        {
            switch (ret)
            {
                case Return.False:
                    return false;
                case Return.True:
                    return true;
                default:
                    throw new Exception();
            }
        });
    }
}

public class ReturnData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { Utility.Return.True };
        yield return new object[] { Utility.Return.False };
        yield return new object[] { Utility.Return.Throw };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}