using GearUpCards.MonoBehaviours;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace GearUpCards.Patches
{
    [HarmonyPatch(typeof(Block))]
    class Block_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Block.IsBlocking))]
        [HarmonyPriority(Priority.Last)]
        static void Block_ModifyIFrame(Block __instance, ref bool __result, CharacterData ___data)
        {
            float factor = 1.0f;

            DesolationStatus status = ___data.player.GetComponent<DesolationStatus>();
            if (status != null)
            {
                factor *= status.GetBlockIFrameMultiplier();
            }

            if (factor <= 0.0f)
            {
                __result = false;
            }
            // other cases are to be implemented and used
        }
    }
}
