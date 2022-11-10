Synthesis provides a side area if you have internal data that a patcher wants access to, but a user does not need to be aware of.

# Internal Data Folder
Create a folder named `InternalData` next to your `.csproj`

`[Path To Solution Folder]/[Path To Project Folder]/InternalData/[... your files ...]`

Any files located in this folder will be accessible by your patcher

# Accessing Internal Data
## TryRetrieve off State
The Synthesis state object has convenience calls to access the internal data by providing the relative paths:
```
// Perhaps pipe this to a Json/xml parser, etc
var pathToInternalFile = state.RetrieveInternalFile("The/Relative/PathYouGaveYourInternalData.exe"));
```
The path given should be relative to the `InternalData` folder, rather than absolute paths.

The function returns the absolute path to the file, or throws with some informative messages if not.

## Manual
The Synthesis state object also contains a `InternalDataPath` member that you can use to inspect and access internal files yourself with normal file API.