# .synth Files are Installer Files
`.synth` files can be made to be "installer" files for patchers.  Users can double click a `.synth` file to have a patcher added to the currently selected group in the Synth UI.

## Using a .synth File to Install a Patcher
- Open Synthesis UI
- Click on the group you want the patcher added to
- Double click and open the `.synth` file

![.synth File](images/synth-files.gif)

## Creating an Installer .synth File
To create a .synth file, create a text file with the repository url and project location that normally shows up. Then make sure to save your file as `[patcher_name].synth`.

```
{
  "AddGitPatcher": 
  {
    "Url": "https://github.com/[Project-Path]",
    "SelectedProject": "[Directory]\\[Project].csproj"
  }
}
```

Users should then be able to use this shortcut that to add your patcher to Synthesis.