using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda
{
    public static class Constants
    {
        public static readonly string SynthesisName = "Synthesis";
        public static readonly ModKey SynthesisModKey = new ModKey(SynthesisName, ModType.Plugin);
        public static readonly string MetaFileName = "SynthesisMeta.json";
        public static readonly string AutomaticListingFileName = "mutagen-automatic-listing.json";
        public static readonly string ListingRepositoryAddress = "https://github.com/Noggog/Synthesis.Registry";
    }
}
