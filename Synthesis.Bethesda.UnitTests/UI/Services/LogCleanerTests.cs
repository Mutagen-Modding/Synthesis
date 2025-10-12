using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.GUI.Logging;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.UI;

public class LogCleanerTests
{
    [Theory, SynthAutoData]
    public void Typical(
        MockFileSystem fs,
        LogCleaner cleaner)
    {
        cleaner.LogSettings.LogFolder.Returns(new DirectoryPath("C:/logs"));
        cleaner.NowProvider.NowLocal.Returns(new DateTime(2021, 9, 16));

        fs.Directory.CreateDirectory("C:/logs");

        // Create a log file from yesterday (should be kept)
        var keepFile = "C:/logs/09-15-2021_10h30m45s.log";
        fs.File.Create(keepFile);

        // Create a log file from 10 days ago (should be deleted)
        var deleteFile = "C:/logs/09-06-2021_10h30m45s.log";
        fs.File.Create(deleteFile);

        // Create Current.log (should always be kept)
        var currentLog = "C:/logs/Current.log";
        fs.File.Create(currentLog);

        cleaner.Start();

        fs.File.Exists(keepFile).ShouldBeTrue();
        fs.File.Exists(deleteFile).ShouldBeFalse();
        fs.File.Exists(currentLog).ShouldBeTrue();
    }
}