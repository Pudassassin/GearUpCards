using HarmonyLib;

using UnityEngine;

using UnboundLib;

using GearUpCards.MonoBehaviours;
using GearUpCards.Extensions;

namespace GearUpCards.Patches
{
    [HarmonyPatch(typeof(DamageOverTime))]
    class DamageOverTime_Patch
    {
        static Color NoTint = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        // private static GameObject empowerShotVFX = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_EmpowerShot");

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch("TakeDamageOverTime")]
        static void ArcaneConvert(HealthHandler ___health, ref Vector2 damage, ref float time, ref float interval, ref Color color, ref Player damagingPlayer, ref bool lethal)
        {
            float arcaneDamage = 0.0f;
            Color tickColor = NoTint;
            float duration = time;
            float tickRate = interval;
            Player creditPlayer = damagingPlayer;
            bool isLethal = lethal;

            if (damagingPlayer != null) 
            { 
                CharacterStatModifiers creditorStat = damagingPlayer.gameObject.GetComponent<CharacterStatModifiers>();
                int stackCount = creditorStat.GetGearData().arcaneConversionStack;
                if (stackCount > 0) 
                { 
                    if (stackCount == 1)
                    {
                        arcaneDamage = damage.magnitude * 0.5f;
                        damage *= 0.5f;
                        tickColor = color;
                    }
                    else if (stackCount >= 2)
                    {
                        arcaneDamage = damage.magnitude;
                        damage = damage.normalized;

                        damagingPlayer = null;

                        tickColor = ArcaneDamageOvertimeEffect.BlinkColor;
                        color = NoTint;
                    }
                }
                else
                { 
                    return;
                }
            }
            else
            { 
                return;
            }

            ArcaneDamageOvertimeEffect arcaneDOT = ___health.gameObject.GetOrAddComponent<ArcaneDamageOvertimeEffect>();
            arcaneDOT.ApplyNewStack(arcaneDamage, duration, tickRate, tickColor, creditPlayer, isLethal);
        }
    }
}