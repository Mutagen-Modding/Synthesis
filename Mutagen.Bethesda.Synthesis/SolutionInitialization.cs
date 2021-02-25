using Loqui;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public static class SolutionInitialization
    {
        public static GetResponse<string> ValidateProjectPath(string projName, GetResponse<string> sln)
        {
            if (string.IsNullOrWhiteSpace(projName)) return GetResponse<string>.Fail("Project needs a name.");
            if (!StringExt.IsViableFilename(projName)) return GetResponse<string>.Fail($"Project had invalid path characters.");
            if (projName.IndexOf(' ') != -1) return GetResponse<string>.Fail($"Project name cannot contain spaces.");

            // Just mark as success until we have one and can analyze further
            if (sln.Failed) return GetResponse<string>.Succeed(string.Empty);

            try
            {
                var projPath = Path.Combine(Path.GetDirectoryName(sln.Value)!, projName, $"{projName}.csproj");
                if (File.Exists(projPath))
                {
                    return GetResponse<string>.Fail($"Target project folder cannot already exist as a file: {projPath}");
                }
                if (Directory.Exists(projPath)
                    && (Directory.EnumerateFiles(projPath).Any()
                    || Directory.EnumerateDirectories(projPath).Any()))
                {
                    return GetResponse<string>.Fail($"Target project folder must be empty: {projPath}");
                }
                return GetResponse<string>.Succeed(projPath);
            }
            catch (ArgumentException)
            {
                return GetResponse<string>.Fail("Improper project name. Go simpler.");
            }
        }

        public static string[] CreateProject(string projPath, GameCategory category, bool insertOldVersion = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
            var projName = Path.GetFileNameWithoutExtension(projPath);

            // Generate Project File
            FileGeneration fg = new FileGeneration();
            fg.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            fg.AppendLine($"  <PropertyGroup>");
            fg.AppendLine($"    <OutputType>Exe</OutputType>");
            fg.AppendLine($"    <TargetFramework>netcoreapp3.1</TargetFramework>");
            fg.AppendLine($"  </PropertyGroup>");
            fg.AppendLine();
            fg.AppendLine($"  <ItemGroup>");
            fg.AppendLine($"    <PackageReference Include=\"Mutagen.Bethesda\" Version=\"{(insertOldVersion ? Versions.OldMutagenVersion : Versions.MutagenVersion)}\" />");
            fg.AppendLine($"    <PackageReference Include=\"Mutagen.Bethesda.Synthesis\" Version=\"{(insertOldVersion ? Versions.OldSynthesisVersion : Versions.SynthesisVersion)}\" />");
            fg.AppendLine($"  </ItemGroup>");
            fg.AppendLine("</Project>");
            fg.Generate(projPath);

            // Generate Program.cs
            fg = new FileGeneration();
            fg.AppendLine("using System;");
            fg.AppendLine("using System.Collections.Generic;");
            fg.AppendLine("using System.Linq;");
            fg.AppendLine("using Mutagen.Bethesda;");
            fg.AppendLine("using Mutagen.Bethesda.Synthesis;");
            fg.AppendLine($"using Mutagen.Bethesda.{category};");
            fg.AppendLine("using System.Threading.Tasks;");
            fg.AppendLine(); 
            fg.AppendLine($"namespace {projName}");
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"public class Program");
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine("public static async Task<int> Main(string[] args)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"return await SynthesisPipeline.Instance");
                        using (new DepthWrapper(fg))
                        {
                            fg.AppendLine($".AddPatch<I{category}Mod, I{category}ModGetter>(RunPatch)");
                            fg.AppendLine($".Run(args, new {nameof(RunPreferences)}()");
                            using (new BraceWrapper(fg) { AppendParenthesis = true, AppendSemicolon = true })
                            {
                                fg.AppendLine($"{nameof(UserPreferences.ActionsForEmptyArgs)} = new {nameof(RunDefaultPatcher)}()");
                                using (new BraceWrapper(fg))
                                {
                                    fg.AppendLine($"{nameof(RunDefaultPatcher.IdentifyingModKey)} = \"YourPatcher.esp\",");
                                    fg.AppendLine($"{nameof(RunDefaultPatcher.TargetRelease)} = {nameof(GameRelease)}.{category.DefaultRelease()},");
                                }
                            }
                        }
                    }
                    fg.AppendLine();

                    fg.AppendLine($"public static void RunPatch({nameof(IPatcherState)}<I{category}Mod, I{category}ModGetter> state)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"//Your code here!");
                    }
                }
            }
            fg.Generate(Path.Combine(Path.GetDirectoryName(projPath)!, "Program.cs"));

            return new string[]
            {
                projPath,
                Path.Combine(Path.GetDirectoryName(projPath)!, "Program.cs")
            };
        }

        public static string[] CreateSolutionFile(string solutionPath)
        {
            var slnDir = Path.GetDirectoryName(solutionPath)!;
            Directory.CreateDirectory(slnDir);

            // Create solution
            FileGeneration fg = new FileGeneration();
            fg.AppendLine($"Microsoft Visual Studio Solution File, Format Version 12.00");
            fg.AppendLine($"# Visual Studio Version 16");
            fg.AppendLine($"VisualStudioVersion = 16.0.30330.147");
            fg.AppendLine($"MinimumVisualStudioVersion = 10.0.40219.1");
            fg.Generate(solutionPath);

            // Create editorconfig
            fg = new FileGeneration();
            fg.AppendLine("[*.cs]");
            fg.AppendLine();
            fg.AppendLine("# CS4014: Task not awaited");
            fg.AppendLine("dotnet_diagnostic.CS4014.severity = error");
            fg.AppendLine();
            fg.AppendLine("# CS1998: Async function does not contain await");
            fg.AppendLine("dotnet_diagnostic.CS1998.severity = silent");
            fg.Generate(Path.Combine(slnDir, ".editorconfig"));

            // Add nullability errors
            fg = new FileGeneration();
            fg.AppendLine("<Project>");
            using (new DepthWrapper(fg))
            {
                fg.AppendLine("<PropertyGroup>");
                using (new DepthWrapper(fg))
                {
                    fg.AppendLine("<Nullable>enable</Nullable>");
                    fg.AppendLine("<WarningsAsErrors>nullable</WarningsAsErrors>");
                }
                fg.AppendLine("</PropertyGroup>");
            }
            fg.AppendLine("</Project>");
            fg.Generate(Path.Combine(slnDir, "Directory.Build.props"));

            return new string[]
            {
                solutionPath,
                Path.Combine(slnDir, ".editorconfig"),
                Path.Combine(slnDir, "Directory.Build.props"),
            };
        }

        public static void AddProjectToSolution(string solutionpath, string projPath)
        {
            var projName = Path.GetFileNameWithoutExtension(projPath);
            File.AppendAllLines(solutionpath,
                $"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"{projName}\", \"{projName}\\{projName}.csproj\", \"{{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}}\"".AsEnumerable()
                .And($"EndProject"));
        }

        public static void GenerateGitIgnore(string folder)
        {
            File.WriteAllText(Path.Combine(folder, ".gitignore"),
@"## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.
##
## Get latest from https://github.com/github/gitignore/blob/master/VisualStudio.gitignore

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Mono auto generated files
mono_crash.*

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/

# Visual Studio 2015/2017 cache/options directory
.vs/
# Uncomment if you have tasks that create the project's static files in wwwroot
#wwwroot/

# Visual Studio 2017 auto generated files
Generated\ Files/

# MSTest test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*

# NUnit
*.VisualState.xml
TestResult.xml
nunit-*.xml

# Build Results of an ATL Project
[Dd]ebugPS/
[Rr]eleasePS/
dlldata.c

# Benchmark Results
BenchmarkDotNet.Artifacts/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/

# StyleCop
StyleCopReport.xml

# Files built by Visual Studio
*_i.c
*_p.c
*_h.h
*.ilk
*.meta
*.obj
*.iobj
*.pch
*.pdb
*.ipdb
*.pgc
*.pgd
*.rsp
*.sbr
*.tlb
*.tli
*.tlh
*.tmp
*.tmp_proj
*_wpftmp.csproj
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc

# Chutzpah Test files
_Chutzpah*

# Visual C++ cache files
ipch/
*.aps
*.ncb
*.opendb
*.opensdf
*.sdf
*.cachefile
*.VC.db
*.VC.VC.opendb

# Visual Studio profiler
*.psess
*.vsp
*.vspx
*.sap

# Visual Studio Trace Files
*.e2e

# TFS 2012 Local Workspace
$tf/

# Guidance Automation Toolkit
*.gpState

# ReSharper is a .NET coding add-in
_ReSharper*/
*.[Rr]e[Ss]harper
*.DotSettings.user

# JustCode is a .NET coding add-in
.JustCode

# TeamCity is a build add-in
_TeamCity*

# DotCover is a Code Coverage Tool
*.dotCover

# AxoCover is a Code Coverage Tool
.axoCover/*
!.axoCover/settings.json

# Visual Studio code coverage results
*.coverage
*.coveragexml

# NCrunch
_NCrunch_*
.*crunch*.local.xml
nCrunchTemp_*

# MightyMoose
*.mm.*
AutoTest.Net/

# Web workbench (sass)
.sass-cache/

# Installshield output folder
[Ee]xpress/

# DocProject is a documentation generator add-in
DocProject/buildhelp/
DocProject/Help/*.HxT
DocProject/Help/*.HxC
DocProject/Help/*.hhc
DocProject/Help/*.hhk
DocProject/Help/*.hhp
DocProject/Help/Html2
DocProject/Help/html

# Click-Once directory
publish/

# Publish Web Output
*.[Pp]ublish.xml
*.azurePubxml
# Note: Comment the next line if you want to checkin your web deploy settings,
# but database connection strings (with potential passwords) will be unencrypted
*.pubxml
*.publishproj

# Microsoft Azure Web App publish settings. Comment the next line if you want to
# checkin your Azure Web App publish settings, but sensitive information contained
# in these scripts will be unencrypted
PublishScripts/

# NuGet Packages
*.nupkg
# NuGet Symbol Packages
*.snupkg
# The packages folder can be ignored because of Package Restore
**/[Pp]ackages/*
# except build/, which is used as an MSBuild target.
!**/[Pp]ackages/build/
# Uncomment if necessary however generally it will be regenerated when needed
#!**/[Pp]ackages/repositories.config
# NuGet v3's project.json files produces more ignorable files
*.nuget.props
*.nuget.targets

# Microsoft Azure Build Output
csx/
*.build.csdef

# Microsoft Azure Emulator
ecf/
rcf/

# Windows Store app package directories and files
AppPackages/
BundleArtifacts/
Package.StoreAssociation.xml
_pkginfo.txt
*.appx
*.appxbundle
*.appxupload

# Visual Studio cache files
# files ending in .cache can be ignored
*.[Cc]ache
# but keep track of directories ending in .cache
!?*.[Cc]ache/

# Others
ClientBin/
~$*
*~
*.dbmdl
*.dbproj.schemaview
*.jfm
*.pfx
*.publishsettings
orleans.codegen.cs

# Including strong name files can present a security risk
# (https://github.com/github/gitignore/pull/2483#issue-259490424)
#*.snk

# Since there are multiple workflows, uncomment next line to ignore bower_components
# (https://github.com/github/gitignore/pull/1529#issuecomment-104372622)
#bower_components/

# RIA/Silverlight projects
Generated_Code/

# Backup & report files from converting an old project file
# to a newer Visual Studio version. Backup files are not needed,
# because we have git ;-)
_UpgradeReport_Files/
Backup*/
UpgradeLog*.XML
UpgradeLog*.htm
ServiceFabricBackup/
*.rptproj.bak

# SQL Server files
*.mdf
*.ldf
*.ndf

# Business Intelligence projects
*.rdl.data
*.bim.layout
*.bim_*.settings
*.rptproj.rsuser
*- [Bb]ackup.rdl
*- [Bb]ackup ([0-9]).rdl
*- [Bb]ackup ([0-9][0-9]).rdl

# Microsoft Fakes
FakesAssemblies/

# GhostDoc plugin setting file
*.GhostDoc.xml

# Node.js Tools for Visual Studio
.ntvs_analysis.dat
node_modules/

# Visual Studio 6 build log
*.plg

# Visual Studio 6 workspace options file
*.opt

# Visual Studio 6 auto-generated workspace file (contains which files were open etc.)
*.vbw

# Visual Studio LightSwitch build output
**/*.HTMLClient/GeneratedArtifacts
**/*.DesktopClient/GeneratedArtifacts
**/*.DesktopClient/ModelManifest.xml
**/*.Server/GeneratedArtifacts
**/*.Server/ModelManifest.xml
_Pvt_Extensions

# Paket dependency manager
.paket/paket.exe
paket-files/

# FAKE - F# Make
.fake/

# CodeRush personal settings
.cr/personal

# Python Tools for Visual Studio (PTVS)
__pycache__/
*.pyc

# Cake - Uncomment if you are using it
# tools/**
# !tools/packages.config

# Tabs Studio
*.tss

# Telerik's JustMock configuration file
*.jmconfig

# BizTalk build output
*.btp.cs
*.btm.cs
*.odx.cs
*.xsd.cs

# OpenCover UI analysis results
OpenCover/

# Azure Stream Analytics local run output
ASALocalRun/

# MSBuild Binary and Structured Log
*.binlog

# NVidia Nsight GPU debugger configuration file
*.nvuser

# MFractors (Xamarin productivity tool) working folder
.mfractor/

# Local History for Visual Studio
.localhistory/

# BeatPulse healthcheck temp database
healthchecksdb

# Backup folder for Package Reference Convert tool in Visual Studio 2017
MigrationBackup/");
        }
    }
}
