using Mutagen.Bethesda.Synthesis.Core.Patchers;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.Core.Runner
{
    public class Runner
    {
        public List<IPatcher> Patchers = new List<IPatcher>();
        public ModPath? SourcePath;
        public DirectoryInfo WorkingDirectory;
        public ModKey ModKey = new ModKey("Synthesis", ModType.Plugin);
        public ModPath OutputPath;

        public Runner(DirectoryInfo workingDir, ModPath outputPath)
        {
            WorkingDirectory = workingDir;
            OutputPath = outputPath;
        }

        public void Run()
        {
            if (SourcePath != null)
            {
                if (!File.Exists(SourcePath))
                {
                    throw new FileNotFoundException($"Could not find defined source path: {SourcePath}");
                }
            }
            WorkingDirectory.DeleteEntireFolder();
            WorkingDirectory.Create();

            if (Patchers.Count == 0) return;

            var prevPath = SourcePath;
            for (int i = 0; i < Patchers.Count; i++)
            {
                var patcher = Patchers[i];
                var nextPath = new ModPath(ModKey, Path.Combine(WorkingDirectory.FullName, $"{i} - {patcher.Name}"));
                patcher.Run(prevPath, nextPath);
                prevPath = nextPath;
            }
            File.Copy(prevPath!.Path, OutputPath);
        }
    }
}
