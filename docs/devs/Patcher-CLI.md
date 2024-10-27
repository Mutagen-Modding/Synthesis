# Patcher CLI

Most of the documentation so far has been focused on making Mutagen-based patchers that are easily addable via Synthesis UI, but any program can be a patcher in Synthesis as long as it conforms to a few standards.

## General Workflow

- Synthesis will pass you command line instructions
- A previous patch file path may be given, which should be built on top of
- A path will be given to export your results to
- Shut down

## Run Patcher CLI
The Synthesis pipeline passes in [command line arguments](https://github.com/Mutagen-Modding/Synthesis/blob/dev/Synthesis.Bethesda/Commands/RunSynthesisPatcher.cs) to request a patch to be made. 

- `-s`/`--SourcePath`: Optional path to the previous patch file to build onto
- `-o`/`--OutputPath`: Path an output patch is expected to be written to
- `-g`/`--GameRelease`: The game the patch is expected to be run on (SkyrimSE/SkyrimLE/Oblivion/etc)
- `-d`/`--DataFolderPath`: The path to the data folder to look for mods.  This may or may not be the typical install folder
- `-l`/`--LoadOrderFilePath`: The path to the load order file to use.  This may or may not be the typical plugins.txt

## Other Commands
There may be other commands Synthesis sends.  Any of these are optional and do not need to be supported in order to be compatible.
