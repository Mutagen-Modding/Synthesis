using CommandLine;
using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda
{
    [Verb("open-for-settings", HelpText = "Informs the patcher to open in settings mode")]
    public class OpenForSettings
    {
        [Option('t', "Top", Required = false, HelpText = "Top location to consider when positioning")]
        public int Top { get; set; }

        [Option('l', "Left", Required = false, HelpText = "Left location to consider when positioning")]
        public int Left { get; set; }

        [Option('w', "Width", Required = false, HelpText = "Width consider when positioning")]
        public int Width { get; set; }

        [Option('h', "Height", Required = false, HelpText = "Height to consider when positioning")]
        public int Height { get; set; }

        public override string ToString()
        {
            return $"{nameof(OpenForSettings)} => \n"
                + $"  {nameof(Top)} => {this.Top} \n"
                + $"  {nameof(Left)} => {this.Left} \n"
                + $"  {nameof(Width)} => {this.Width} \n"
                + $"  {nameof(Height)} => {this.Height}";
        }
    }
}
