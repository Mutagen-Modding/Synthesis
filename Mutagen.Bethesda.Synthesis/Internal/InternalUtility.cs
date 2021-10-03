using Mutagen.Bethesda.Plugins.Records;
using System;

namespace Mutagen.Bethesda.Synthesis.Internal
{
    public class Utility
    {
        public static GameCategory TypeToGameCategory<TMod>()
            where TMod : IModGetter
        {
            switch (typeof(TMod).Name)
            {
                case "ISkyrimMod":
                case "ISkyrimModGetter":
                    return GameCategory.Skyrim;
                case "IOblivionMod":
                case "IOblivionModGetter":
                    return GameCategory.Oblivion;
                default:
                    throw new ArgumentException($"Unknown game type for: {typeof(TMod).Name}");
            }
        }
    }
}
