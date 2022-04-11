using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Mutagen.Bethesda.Synthesis.WPF;

public interface ICopyDirectory
{
    void Copy(string source, string target, CancellationToken cancel);
}

public class CopyDirectory : ICopyDirectory
{
    public void Copy(string source, string target, CancellationToken cancel)
    {
        var stack = new Stack<Folders>();
        stack.Push(new Folders(source, target));

        while (stack.Count > 0)
        {
            if (cancel.IsCancellationRequested) return;
            var folders = stack.Pop();
            Directory.CreateDirectory(folders.Target);
            foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
            {
                if (cancel.IsCancellationRequested) return;
                File.Copy(file, Path.Combine(folders.Target, Path.GetFileName(file)));
            }

            foreach (var folder in Directory.GetDirectories(folders.Source))
            {
                if (cancel.IsCancellationRequested) return;
                stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
            }
        }
    }

    class Folders
    {
        public string Source { get; private set; }
        public string Target { get; private set; }

        public Folders(string source, string target)
        {
            Source = source;
            Target = target;
        }
    }
}