using System;
using HarmonyLib;

using UnityEngine;

using GearUpCards.MonoBehaviours;

namespace GearUpCards.Patches
{
    // Fix trails size scaling up too fast with damage
    [HarmonyPatch(typeof(ScaleTrailFromDamage))]
    class ScaleTrailFromDamage_Patch
    {
        public static float LogBase = 2.2f;     // was 1.5f
        public static float MinSize = 0.25f;    // was 1.0f
        public static float MaxSize = 25f;
        public static float XdmgShift = 1.0f;   // was 0.0f
        public static float YscaleShift = 1.0f; // was 2.0f
        public static float DivValue = 2.0f;    // was 2.0f

        public static float DetailThreshold = 550f;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("Start")]
        static bool Start_Override(ScaleTrailFromDamage __instance, ref float ___startWidth, ref float ___startTime)
        {
            float damage = 55f;
            ProjectileHit projectileHit = __instance.gameObject.GetComponentInParent<ProjectileHit>();
            // GetComponentInParent<RayCastTrail>();
            if ((bool)projectileHit)
            {
                damage = projectileHit.damage;

                ArcaneConversionModifier arcaneMod = projectileHit.gameObject.GetComponentInChildren<ArcaneConversionModifier>();
                if (arcaneMod != null)
                {
                    damage += arcaneMod.arcaneDamage;
                }
            }

            TrailRenderer trailRenderer = __instance.gameObject.GetComponentInChildren<TrailRenderer>();
            ___startWidth = trailRenderer.widthMultiplier;
            ___startTime = trailRenderer.time;
            if ((bool)trailRenderer)
            {
                float dmgScale = (YscaleShift + Mathf.Log(XdmgShift + (damage / 55f), LogBase)) / DivValue;

                trailRenderer.widthMultiplier = ___startWidth * Mathf.Clamp(dmgScale, MinSize, MaxSize);
                trailRenderer.time = ___startTime * Mathf.Clamp(dmgScale, 0f, 25f);

                if (damage > DetailThreshold)
                {
                    trailRenderer.numCapVertices = 10;
                }
                else
                {
                    trailRenderer.numCapVertices = 5;
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("Rescale")]
        static bool Rescale_Override(ScaleTrailFromDamage __instance, float ___startWidth, float ___startTime)
        {
            float damage = 55f;
            ProjectileHit projectileHit = __instance.gameObject.GetComponentInParent<ProjectileHit>();
            RayCastTrail RayCastTrail = __instance.gameObject.GetComponentInParent<RayCastTrail>();
            if ((bool)projectileHit)
            {
                damage = projectileHit.damage;

                ArcaneConversionModifier arcaneMod = projectileHit.gameObject.GetComponentInChildren<ArcaneConversionModifier>();
                if (arcaneMod != null)
                {
                    damage += arcaneMod.arcaneDamage;
                }
            }

            TrailRenderer trailRenderer = __instance.gameObject.GetComponentInChildren<TrailRenderer>();

            if ((bool)trailRenderer)
            {
                float dmgScale = (YscaleShift + Mathf.Log(XdmgShift + (damage / 55f), LogBase)) / DivValue;

                _ = RayCastTrail.extraSize;
                trailRenderer.widthMultiplier = ___startWidth * Mathf.Clamp(dmgScale, MinSize, MaxSize);
                trailRenderer.time = ___startTime * Mathf.Clamp(dmgScale, 0f, 25f);
                
                if (damage > DetailThreshold)
                {
                    trailRenderer.numCapVertices = 10;
                }
                else
                {
                    trailRenderer.numCapVertices = 5;
                }
            }

            return false;
        }
    }

    // Fix bullet collision size to go along with visual, and not to big too fast (Careful!!!)
    [HarmonyPatch(typeof(RayCastTrail))]
    class RayCastTrail_Patch
    {
        public static float LogBase = 2.2f;     // was 1.5f
        public static float MinSize = 0.3f;     // vanilla default (!!!)
        public static float MaxSize = 25f;
        public static float XdmgShift = 2.5f;   // was 0.0f
        public static float YscaleShift = 0.0f; // was 2.0f
        public static float DivValue = 4.0f;    // was 2.0f

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("Start")]
        static void Start_Override(RayCastTrail __instance, ref float ___size, float ___extraSize)
        {
            float damage = 55f;
            ProjectileHit projectileHit = __instance.gameObject.GetComponent<ProjectileHit>();
            if (projectileHit != null)
            {
                damage = projectileHit.damage;

                ArcaneConversionModifier arcaneMod = projectileHit.gameObject.GetComponentInChildren<ArcaneConversionModifier>();
                if (arcaneMod != null)
                {
                    damage += arcaneMod.arcaneDamage;
                }

                ___size = Mathf.Clamp((YscaleShift + Mathf.Log(XdmgShift + (damage / 55f), LogBase)) / DivValue, MinSize, MaxSize) + ___extraSize;
            }
        }
    }

    // Fix particle vfx size scaling up too fast with damage
    [HarmonyPatch(typeof(RescaleFromDamagePart))]
    class RescaleFromDamagePart_Patch
    {
        public static float LogBase = 2.5f;     // was 5.0f
        public static float MinSize = 1f;
        public static float MaxSize = 25f;
        public static float XdmgShift = 2.5f;   // was 0.0f
        public static float YscaleShift = 0.0f; // was 2.0f
        public static float DivValue = 4.0f;    // was 2.0f

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("Rescale")]
        static bool Rescale_Override(RescaleFromDamagePart __instance, ref ProjectileHit ___hit, ParticleSystem ___part, float ___startSize)
        {
            float damage;

            if (!___hit)
            {
                ___hit = __instance.GetComponentInParent<ProjectileHit>();
            }

            damage = ___hit.damage;
            ArcaneConversionModifier arcaneMod = ___hit.gameObject.GetComponentInChildren<ArcaneConversionModifier>();
            if (arcaneMod != null)
            {
                damage += arcaneMod.arcaneDamage;
            }

            if ((bool)___hit)
            {
                float dmgScale = (YscaleShift + Mathf.Log(XdmgShift + (damage / 55f), LogBase)) / DivValue;

                ___part.startSize = ___startSize * Mathf.Clamp(dmgScale, MinSize, MaxSize);
            }

            return false;
        }
    }
}
