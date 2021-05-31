using System;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.EnvironmentErrors
{
    public class EnvironmentNuget_Tests
    {
        #region Triggers

        [Fact]
        public void CorruptTrigger()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "Whut");
            NugetErrors.AnalyzeNugetConfig(path)
                .Should().BeOfType<CorruptError>();
        }
        
        [Fact]
        public void MissingTrigger()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            NugetErrors.AnalyzeNugetConfig(path)
                .Should().BeOfType<NotExistsError>();
        }
        
        [Fact]
        public void EmptyFileTrigger()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "");
            NugetErrors.AnalyzeNugetConfig(path)
                .Should().BeOfType<NotExistsError>();
        }
        
        [Fact]
        public void NoConfigurationTrigger()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<something />");
            NugetErrors.AnalyzeNugetConfig(path)
                .Should().BeOfType<NotExistsError>();
        }
        
        [Fact]
        public void EmptyPackageSourcesTrigger()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                    "<configuration>" +
                                    "<packageSources>" +
                                    "</packageSources>" +
                                    "</configuration>");
            NugetErrors.AnalyzeNugetConfig(path)
                .Should().BeOfType<MissingNugetOrgError>();
        }
        
        [Fact]
        public void OtherPackageSourcesTrigger()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                    "<configuration>" +
                                    "<packageSources>" +
                                    "<add key=\"CSharp Dev\" value=\"C:\\Repos\\CSharpExt\\Noggog.CSharpExt\\bin\\Debug\" />" +
                                    "</packageSources>" +
                                    "</configuration>");
            NugetErrors.AnalyzeNugetConfig(path)
                .Should().BeOfType<MissingNugetOrgError>();
        }
        
        [Fact]
        public void MissingPackageSourcesTrigger()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                    "<configuration>" +
                                    "</configuration>");
            NugetErrors.AnalyzeNugetConfig(path)
                .Should().BeOfType<MissingNugetOrgError>();
        }

        #endregion

        #region Fixes

        [Fact]
        public void CorruptFix()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "Whut");
            var err = new CorruptError(new Exception());
            err.RunFix(path);
            var doc = XDocument.Load(path);
            doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
        }
        
        [Fact]
        public void MissingFix()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            var err = NotExistsError.Instance;
            err.RunFix(path);
            var doc = XDocument.Load(path);
            doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
        }
        
        [Fact]
        public void EmptyFileFix()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "");
            var err = NotExistsError.Instance;
            err.RunFix(path);
            var doc = XDocument.Load(path);
            doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
        }
        
        [Fact]
        public void NoConfigurationFix()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<something />");
            var err = NotExistsError.Instance;
            err.RunFix(path);
            var doc = XDocument.Load(path);
            doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
        }
        
        [Fact]
        public void EmptyPackageSourcesFix()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                    "<configuration>" +
                                    "<packageSources>" +
                                    "</packageSources>" +
                                    "</configuration>");
            var err = MissingNugetOrgError.Instance;
            err.RunFix(path);
            var doc = XDocument.Load(path);
            doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
        }
        
        [Fact]
        public void OtherPackageSourcesFix()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                    "<configuration>" +
                                    "<packageSources>" +
                                    "<add key=\"CSharp Dev\" value=\"C:\\Repos\\CSharpExt\\Noggog.CSharpExt\\bin\\Debug\" />" +
                                    "</packageSources>" +
                                    "</configuration>");
            var err = MissingNugetOrgError.Instance;
            err.RunFix(path);
            var doc = XDocument.Load(path);
            var elem = new XElement("configuration",
                new XElement("packageSources",
                    new XElement("add",
                        new XAttribute("key", "CSharp Dev"),
                        new XAttribute("value", "C:\\Repos\\CSharpExt\\Noggog.CSharpExt\\bin\\Debug")),
                new XElement("add",
                    new XAttribute("key", "nuget.org"),
                    new XAttribute("value", "https://api.nuget.org/v3/index.json"),
                    new XAttribute("protocolVersion", "3"))));
            var expected = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                elem);
            doc.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void MissingPackageSourcesFix()
        {
            using var tempFolder = Utility.GetTempFolder(nameof(EnvironmentNuget_Tests));
            FilePath path = Path.Combine(tempFolder.Dir.Path, "Nuget.Config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                    "<configuration>" +
                                    "</configuration>");
            var err = MissingNugetOrgError.Instance;
            err.RunFix(path);
            var doc = XDocument.Load(path);
            doc.Should().BeEquivalentTo(NotExistsError.TypicalFile());
        }

        #endregion
    }
}