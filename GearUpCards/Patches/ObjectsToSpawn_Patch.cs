using System;
using HarmonyLib;

using UnityEngine;

using GearUpCards.MonoBehaviours;

namespace GearUpCards.Patches
{
    [HarmonyPatch(typeof(ObjectsToSpawn))]
    class ObjectsToSpawn_Patch
    {
        // (Transform spawnerTransform, HitInfo hit, ObjectsToSpawn objectToSpawn, HealthHandler playerHealth, PlayerSkin playerSkins, float damage = 55f, SpawnedAttack spawnedAttack = null, bool wasBlocked = false)
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch("SpawnObject")]
        [HarmonyPatch(new Type[]{typeof(Transform), typeof(HitInfo), typeof(ObjectsToSpawn), typeof(HealthHandler), typeof(PlayerSkin), typeof(float), typeof(SpawnedAttack), typeof(bool) })]
        static void ArcaneScaling(ref Transform spawnerTransform, ref float damage)
        {
            ArcaneConversionModifier arcaneMod = spawnerTransform.gameObject.GetComponentInChildren<ArcaneConversionModifier>();
            ProjectileHit projectileHit = spawnerTransform.GetComponent<ProjectileHit>();

            if (arcaneMod != null)
            {
                damage += arcaneMod.arcaneDamage;
            }
        }
    }
}