using System.IO.Abstractions;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.Testing.AutoFixture;
using Serilog;
using Serilog.Core;
using StrongInject;
using Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.ModifyProject;

[Register(typeof(ModifyRunnerProjects))]
[Register(typeof(SwapInDesiredVersionsForProjectString), typeof(ISwapInDesiredVersionsForProjectString))]
[Register(typeof(ProvideCurrentVersions), typeof(IProvideCurrentVersions))]
[Register(typeof(TurnOffNullability), typeof(ITurnOffNullability))]
[Register(typeof(ProcessProjUsings), typeof(IProcessProjUsings))]
[Register(typeof(RemoveGitInfo), typeof(IRemoveGitInfo))]
[Register(typeof(RemoveProject), typeof(IRemoveProject))]
[Register(typeof(SwapVersioning), typeof(ISwapVersioning))]
[Register(typeof(AddNewtonsoftToOldSetups), typeof(IAddNewtonsoftToOldSetups))]
[Register(typeof(AvailableProjectsRetriever), typeof(IAvailableProjectsRetriever))]
[Register(typeof(SwapToProperNetVersion), typeof(ISwapToProperNetVersion))]
[Register(typeof(AddAllReleasesToOldVersions), typeof(AddAllReleasesToOldVersions))]
[Register(typeof(TurnOffWindowsSpecificationInTargetFramework), typeof(ITurnOffWindowsSpecificationInTargetFramework))]
partial class ModifyRunnerProjectsContainer : IContainer<ModifyRunnerProjects>
{
	[Instance] private readonly IFileSystem _fileSystem;
	[Instance] private readonly ILogger _logger = Logger.None;

	public ModifyRunnerProjectsContainer(IFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
	}
}

public class ModifyRunnerProjectsTests
{
	private const string Sln =
		"""
		Microsoft Visual Studio Solution File, Format Version 12.00
		# Visual Studio Version 16
		VisualStudioVersion = 16.0.30611.23
		MinimumVisualStudioVersion = 10.0.40219.1
		Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SomeProj", "SomeProj\SomeProj.csproj", "{E71E62D8-9858-4152-815C-81BC794FC538}"
		EndProject
		Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Solution Items", "Solution Items", "{A8698E70-C70D-4394-8E37-675B3032E862}"
			ProjectSection(SolutionItems) = preProject
				.editorconfig = .editorconfig
			EndProjectSection
		EndProject
		Global
			GlobalSection(SolutionConfigurationPlatforms) = preSolution
				Debug|Any CPU = Debug|Any CPU
				Debug|x64 = Debug|x64
				Debug|x86 = Debug|x86
				Release|Any CPU = Release|Any CPU
				Release|x64 = Release|x64
				Release|x86 = Release|x86
			EndGlobalSection
			GlobalSection(ProjectConfigurationPlatforms) = postSolution
				{E71E62D8-9858-4152-815C-81BC794FC538}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
				{E71E62D8-9858-4152-815C-81BC794FC538}.Debug|Any CPU.Build.0 = Debug|Any CPU
				{E71E62D8-9858-4152-815C-81BC794FC538}.Debug|x64.ActiveCfg = Debug|x64
				{E71E62D8-9858-4152-815C-81BC794FC538}.Debug|x64.Build.0 = Debug|x64
				{E71E62D8-9858-4152-815C-81BC794FC538}.Debug|x86.ActiveCfg = Debug|Any CPU
				{E71E62D8-9858-4152-815C-81BC794FC538}.Debug|x86.Build.0 = Debug|Any CPU
				{E71E62D8-9858-4152-815C-81BC794FC538}.Release|Any CPU.ActiveCfg = Release|Any CPU
				{E71E62D8-9858-4152-815C-81BC794FC538}.Release|Any CPU.Build.0 = Release|Any CPU
				{E71E62D8-9858-4152-815C-81BC794FC538}.Release|x64.ActiveCfg = Release|x64
				{E71E62D8-9858-4152-815C-81BC794FC538}.Release|x64.Build.0 = Release|x64
				{E71E62D8-9858-4152-815C-81BC794FC538}.Release|x86.ActiveCfg = Release|Any CPU
				{E71E62D8-9858-4152-815C-81BC794FC538}.Release|x86.Build.0 = Release|Any CPU
			EndGlobalSection
			GlobalSection(SolutionProperties) = preSolution
				HideSolutionNode = FALSE
			EndGlobalSection
			GlobalSection(ExtensibilityGlobals) = postSolution
				SolutionGuid = {DC8750D8-32B5-4829-8394-439B5A459650}
			EndGlobalSection
		EndGlobal
		""";

	private const string NetCoreProj =
		"""
		<Project Sdk="Microsoft.NET.Sdk">
		
		  <PropertyGroup>
		    <OutputType>Exe</OutputType>
		    <TargetFramework>net5.0</TargetFramework>
		  </PropertyGroup>
		
		  <ItemGroup>
		    <PackageReference Include="Mutagen.Bethesda" Version="0.43.0" />
		    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="0.28" />
		  </ItemGroup>

		</Project>
		""";

	[Theory, DefaultAutoData]
	public async Task Typical(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, NetCoreProj);
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.45",
				"0.30"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}

	[Theory, DefaultAutoData]
	public async Task Legacy(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, NetCoreProj);
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.44",
				"0.29"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}
	
	[Theory, DefaultAutoData]
	public async Task NetCoreApp(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, 
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			
			  <PropertyGroup>
			    <OutputType>Exe</OutputType>
			    <TargetFramework>netcoreapp3.1</TargetFramework>
			  </PropertyGroup>
			
			  <ItemGroup>
			    <PackageReference Include="Mutagen.Bethesda" Version="0.19.0" />
			    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="0.10.10" />
			  </ItemGroup>
			
			</Project>
			""");
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.44",
				"0.29"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}
	
	[Theory, DefaultAutoData]
	public async Task TurnOffNullability(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, 
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			
			  <PropertyGroup>
			    <OutputType>Exe</OutputType>
				<TargetFramework>net6.0</TargetFramework>
				<WarningsAsErrors>nullable</WarningsAsErrors>
			  </PropertyGroup>
			
			  <ItemGroup>
			    <PackageReference Include="Mutagen.Bethesda" Version="0.19.0" />
			    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="0.10.10" />
			  </ItemGroup>

			</Project>
			""");
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.44",
				"0.29"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}
	
	[Theory, DefaultAutoData]
	public async Task RemoveGitInfo(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, 
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			
			  <PropertyGroup>
			    <OutputType>Exe</OutputType>
				<TargetFramework>net6.0</TargetFramework>
			  </PropertyGroup>
			
			  <ItemGroup>
				<PackageReference Include="GitInfo" Version="0.19.0" />
				<PackageReference Include="Mutagen.Bethesda" Version="0.19.0" />
			    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="0.10.10" />
			  </ItemGroup>

			</Project>
			""");
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.44",
				"0.29"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}
	
	[Theory, DefaultAutoData]
	public async Task RemoveWindowsSpec(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, 
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			
			  <PropertyGroup>
			    <OutputType>Exe</OutputType>
				<TargetFramework>net6.0-windows7.0</TargetFramework>
			  </PropertyGroup>
			
			  <ItemGroup>
				<PackageReference Include="Mutagen.Bethesda" Version="0.19.0" />
			    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="0.10.10" />
			  </ItemGroup>

			</Project>
			""");
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.44",
				"0.29"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}
	
	[Theory, DefaultAutoData]
	public async Task TrimVersion(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, 
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			
			  <PropertyGroup>
			    <OutputType>Exe</OutputType>
				<TargetFramework>net6.0</TargetFramework>
			  </PropertyGroup>
			
			  <ItemGroup>
				<PackageReference Include="Mutagen.Bethesda" Version="0.19.0" />
			    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="0.10.10" />
			  </ItemGroup>

			</Project>
			""");
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.44-abc",
				"0.29-abc"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}
	
	[Theory, DefaultAutoData]
	public async Task AddMutagenToOlderVersions(
		IFileSystem fileSystem,
		FilePath existingSlnPath)
	{
		var subPath = Path.Combine("SomeProj", "SomeProj.csproj");
		fileSystem.File.WriteAllText(existingSlnPath, Sln);
		var projPath = Path.Combine(Path.GetDirectoryName(existingSlnPath)!, subPath);
		fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
		fileSystem.File.WriteAllText(projPath, 
			"""
			<Project Sdk="Microsoft.NET.Sdk">
			
			  <PropertyGroup>
			    <OutputType>Exe</OutputType>
				<TargetFramework>net6.0</TargetFramework>
			  </PropertyGroup>
			
			  <ItemGroup>
			    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="0.10.10" />
			  </ItemGroup>

			</Project>
			""");
		var sut = new ModifyRunnerProjectsContainer(fileSystem);
		sut.Resolve().Value.Modify(
			existingSlnPath, 
			subPath, 
			new NugetVersionPair(
				"0.45",
				"0.29"),
			out var pair);
		await Verify(fileSystem.File.ReadAllText(projPath));
	}
}