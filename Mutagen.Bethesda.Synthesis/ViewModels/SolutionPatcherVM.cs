using Mutagen.Bethesda.Synthesis.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class SolutionPatcherVM : PatcherVM
    {
        public SolutionPatcherVM(MainVM mvm)
            : base(mvm)
        {
        }

        public SolutionPatcherVM(MainVM mvm, SolutionPatcherSettings settings)
            : this(mvm)
        {
        }

        public override PatcherSettings Save()
        {
            return new SolutionPatcherSettings();
        }
    }
}
