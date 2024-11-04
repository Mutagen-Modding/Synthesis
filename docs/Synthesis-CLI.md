# Synthesis CLI

Synthesis offers a CLI alternative to the UI.  This can be useful for Linux users, or anyone else that wants to do operations without the UI.

## Run Pipeline
`run-pipeline`

Runs the Synthesis pipeline on all the groups for a specific profile.

### Typical
`.\Path\To\Synthesis.CLI.exe run-pipeline --OutputDirectory "C:\Games\steamapps\common\Skyrim Special Edition\Data" --ProfileDefinitionPath ".\Path\To\PipelineSettings.json" --ProfileName "NameOfProfile"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-o` | `--OutputDirectory` | Required | Path where the patcher should place its resulting file(s). |
| `-p` | `--ProfileDefinitionPath` | Required | Path to a specific profile or settings definition to run |
| `-d` | `--DataFolderPath` | Optional | Path to the data folder. |
| `-n` | `--ProfileName` | Semi-Optional | Nickname/GUID of profile to run if path is to a settings file with multiple profiles |
| `-l` | `--LoadOrderFilePath` | Optional | Path to the load order file to use. |
| `-e` | `--ExtraDataFolder` | Optional | Path to where top level extra patcher data should be stored/read from.  Default is next to the exe |
| `-r` | `--PersistencePath` | Optional | Path to the shared FormKey allocation state |
| `-m` | `--PersistenceMode` | Optional | Path to the Persistence state style to use |
| `-t` | `--TargetRuntime` | Optional | Target runtime to specify explicitly |

## Create Profile
`create-profile`

Creates a new profile and saves it to the settings

### Typical
`.\Path\To\Synthesis.CLI.exe create-profile --GameRelease SkyrimSE --ProfileName "New Profile Name" --InitialGroupName "New Group Name" --SettingsFolderPath "\Path\To\Settings\"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-r` | `--GameRelease` | Required | Game release that the profile should be related to |
| `-n` | `--ProfileName` | Required | Name to give profile |
| `-g` | `--InitialGroupName` | Required | Name to give the initial patcher group |
| `-p` | `--SettingsFolderPath` | Required | Path to the folder containing the PipelineSettings.json to be adjusted |

## Create Patcher
`create-profile`

Create a new patcher project template.  This is a new folder with a csproj, solution, and other related bits to getting started developing a new patcher.

### Typical
`.\Path\To\Synthesis.CLI.exe create-patcher --GameCategory Skyrim --ParentDirectory "Path\To\Some\Dir" --PatcherName "My First Patcher"`

### Parameters

| Short | Long | Required | Description |
| ---- | ---- | ---- | ---- |
| `-c` | `--GameCategory` | Required | Game category that the patcher should be related to |
| `-d` | `--ParentDirectory` | Required | Parent directory to house new solution folder |
| `-n` | `--PatcherName` | Required | Name to give patcher |
