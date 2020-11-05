using System;
using System.Collections.Generic;
using System.Text;
using Wabbajack.Common;

namespace Mutagen.Bethesda.Synthesis
{
    public static class WjTranslation
    {
        public static Game ToWjGame(this GameRelease release)
        {
            return release switch
            {
                GameRelease.Oblivion => Game.Oblivion,
                GameRelease.SkyrimLE => Game.Skyrim,
                GameRelease.SkyrimSE => Game.SkyrimSpecialEdition,
                GameRelease.SkyrimVR => Game.SkyrimVR,
                _ => throw new NotImplementedException()
            };
        }
    }
}
