using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.harmony
{
    [HarmonyPatch]
    public static class harmPatches
    {
        public static bool Prefix_ModSystemShouldLoad(WorldMapManager __instance, EnumAppSide side, ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
