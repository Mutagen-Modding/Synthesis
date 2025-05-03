﻿using System.IO.Abstractions.TestingHelpers;
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
        cleaner.LogSettings.DateFormat.Returns(Log.DateFormat);
        cleaner.NowProvider.NowLocal.Returns(new DateTime(2021, 9, 16));
            
        fs.Directory.CreateDirectory("C:/logs");
        var keepDir = $"C:/logs/{new DateTime(2021, 9, 15).ToString(Log.DateFormat)}";
        fs.Directory.CreateDirectory($"C:/logs/{new DateTime(2021, 9, 15).ToString(Log.DateFormat)}");
        var keepFile = Path.Combine(keepDir, "SomeFile");
        fs.File.Create(keepFile);
        var deleteDir = $"C:/logs/{new DateTime(2021, 9, 6).ToString(Log.DateFormat)}";
        fs.Directory.CreateDirectory(deleteDir);
        var deleteFile = Path.Combine(deleteDir, "SomeFile");
        fs.File.Create(deleteFile);
            
        cleaner.Start();

        fs.Directory.Exists(keepDir).ShouldBeTrue();
        fs.File.Exists(keepFile).ShouldBeTrue();
        fs.Directory.Exists(deleteDir).ShouldBeFalse();
        fs.File.Exists(deleteFile).ShouldBeFalse();
    }
}