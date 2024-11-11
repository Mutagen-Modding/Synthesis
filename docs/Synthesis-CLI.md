# Synthesis CLI

Synthesis offers a CLI alternative to the UI.  This can be useful for Linux users, or anyone else that wants to do operations without the UI.

## Run Pipeline
`run-pipeline`

Runs the Synthesis pipeline on all the groups for a specific profile.

### Typical
`.\Path\To\Synthesis.CLI.exe run-pipeline --OutputDirectory "C:\Games\steamapps\common\Skyrim Special Edition\Data" --PipelineSettingsPath ".\Path\To\PipelineSettings.json" --ProfileIdentifier "NameOfProfile"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-o` | `--OutputDirectory` | Required | Path where the patcher should place its resulting file(s). |
| `-s` | `--PipelineSettingsPath` | Required | Path to a specific pipeline settings to read from |
| `-d` | `--DataFolderPath` | Optional | Path to the data folder. |
| `-p` | `--ProfileIdentifier` | Semi-Optional | Nickname/GUID of profile to run if path is to a settings file with multiple profiles |
| `-l` | `--LoadOrderFilePath` | Optional | Path to the load order file to use. |
| `-e` | `--ExtraDataFolder` | Optional | Path to where top level extra patcher data should be stored/read from.  Default is next to the exe |
| `-r` | `--PersistencePath` | Optional | Path to the shared FormKey allocation state |
| `-m` | `--PersistenceMode` | Optional | Path to the Persistence state style to use |
| `-t` | `--TargetRuntime` | Optional | Target runtime to specify explicitly |

## Create Profile
`create-profile`

Creates a new profile and saves it to the settings

### Typical
`.\Path\To\Synthesis.CLI.exe create-profile --GameRelease SkyrimSE --ProfileName "New Profile Name" --InitialGroupName "New Group Name" --PipelineSettingsPath ".\Path\To\PipelineSettings.json"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-r` | `--GameRelease` | Required | Game release that the profile should be related to |
| `-p` | `--ProfileIdentifier` | Semi-Optional | Nickname/GUID of profile to run if path is to a settings file with multiple profiles |
| `-g` | `--InitialGroupName` | Required | Name to give the initial patcher group |
| `-s` | `--PipelineSettingsPath` | Required | Path to a specific pipeline settings to target |

## Add Git Patcher
`add-git-patcher`

Adds a Git patcher to a profile and saves it to the settings.

Main parameters are the Github URL address of the patcher to add, as well as the project subpath.  The project subpath is the subpath needed to go from the top folder of the patcher's source code to the `.csproj` to run as the patcher.

### Typical
`.\Path\To\Synthesis.CLI.exe add-git-patcher --ProfileIdentifier "Profile Name" --GroupName "Group Name" --GitRepoAddress "UrlOfPatcherGithub" --ProjectSubPath "Project/Project.csproj" --PipelineSettingsPath ".\Path\To\PipelineSettings.json"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-p` | `--ProfileIdentifier` | Semi-Optional | Nickname/GUID of profile to add to if path is to a settings file with multiple profiles |
| `-g` | `--GroupName` | Required | Name of the patcher group to add the patcher to |
| `-s` | `--PipelineSettingsPath` | Required | Path to a specific pipeline settings to target |
| `-a` | `--GitRepoAddress` | Required | URL address to the repository to add the git patcher from |
|  | `--ProjectSubpath` | Required | Project subpath to target |
|  | `--PatcherNickname` | Optional | Nickname to give the patcher's entry |

## Add Solution Patcher
`add-solution-patcher`

Adds a Solution patcher to a profile and saves it to the settings.

### Typical
`.\Path\To\Synthesis.CLI.exe add-solution-patcher --ProfileIdentifier "Profile Name" --GroupName "Group Name" --SolutionPath ".\Path\To\Solution.sln" --ProjectSubPath "Project/Project.csproj" --PipelineSettingsPath ".\Path\To\PipelineSettings.json"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-p` | `--ProfileIdentifier` | Semi-Optional | Nickname/GUID of profile to add to if path is to a settings file with multiple profiles |
| `-g` | `--GroupName` | Required | Name of the patcher group to add the patcher to |
| `-s` | `--PipelineSettingsPath` | Required | Path to a specific pipeline settings to target |
|  | `--SolutionPath` | Required | Path to the solution to target |
|  | `--ProjectSubpath` | Required | Project subpath to target |
|  | `--PatcherNickname` | Optional | Nickname to give the patcher's entry |

## Create Template Patcher
`create-template-patcher`

Create a new patcher project template for a developer to start creating a new patcher.  This is a new folder with a csproj, solution, and other related bits to getting started developing a new patcher.

### Typical
`.\Path\To\Synthesis.CLI.exe create-template-patcher --GameCategory Skyrim --ParentDirectory "Path\To\Some\Dir" --PatcherName "My First Patcher"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-c` | `--GameCategory` | Required | Game category that the patcher should be related to |
| `-d` | `--ParentDirectory` | Required | Parent directory to house new solution folder |
| `-n` | `--PatcherName` | Required | Name to give patcher |
