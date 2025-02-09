using GearUpCards.Utils;
using ModdingUtils.MonoBehaviours;
using System;
using UnboundLib;
using UnityEngine;

using GearUpCards.Extensions;

namespace GearUpCards.MonoBehaviours
{
    public class OrbObliterationModifier : RayHitEffect
    {
        private static GameObject VFXPrefab = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_OrbLiterationImpact");

        private float healthCullAreaHit = 0.80f;
        private float healthCullDirectHit = 0.65f;
        private float obliterationRadius = 5.0f;

        // private Gun shooterGun;
        // private Player shooterPlayer;

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            if (hit.transform == null)
            {
                return HasToReturn.canContinue;
            }
            // if (hit.transform.gameObject.tag.Contains("Bullet"))
            // {
            //     return HasToReturn.canContinue;
            // }

            bool hitPLayer = false;
            HollowLifeEffect status;
            float playerDistance;

            ProjectileHit projectileHit = this.gameObject.GetComponentInParent<ProjectileHit>();
            CharacterStatModifiers shooterStats = projectileHit.ownPlayer.gameObject.GetComponent<CharacterStatModifiers>();
            float glyphPotency = shooterStats.GetGearData().glyphPotency;
            float glyphInfluence = shooterStats.GetGearData().glyphInfluence;

            float effectRadius = obliterationRadius + (1.0f * glyphInfluence);

            Player victimPlayer = hit.transform.GetComponent<Player>();
            int victimID = -1;


            if (victimPlayer)
            {
                CharacterStatModifiers victimStats = victimPlayer.gameObject.GetComponent<CharacterStatModifiers>();
                int protectionLvl = victimStats.GetGearData().glyphProtection;

                // direct hit victim take more MAX HP loss
                GameObject victim = victimPlayer.gameObject;
                float effectValue = healthCullDirectHit - (0.10f * glyphPotency);
                effectValue = Mathf.Clamp(effectValue, 0.05f, 1.0f);

                float tValue = Mathf.Pow(0.90f, protectionLvl);
                float lerp = Mathf.Lerp(1.0f, effectValue, tValue);

                status = victim.GetOrAddComponent<HollowLifeEffect>();

                if (protectionLvl > 0)
                {
                    status.ApplyTempHealthCap(lerp);
                    victimPlayer.data.health *= lerp;
                }
                else
                {
                    status.ApplyTempHealthCap(effectValue);
                    victimPlayer.data.health *= effectValue;
                }

                hitPLayer = true;
                victimID = victimPlayer.playerID;
                // return HasToReturn.canContinue;
            }

            // apply MAX HP Culling
            foreach (Player target in PlayerManager.instance.players)
            {
                if (target.playerID == victimID)
                {
                    continue;
                }

                playerDistance = (target.transform.position - gameObject.transform.position).magnitude;
                if (playerDistance <= effectRadius)
                {
                    CharacterStatModifiers victimStats = target.gameObject.GetComponent<CharacterStatModifiers>();
                    int protectionLvl = victimStats.GetGearData().glyphProtection;

                    float effectValue = healthCullAreaHit - (0.05f * glyphPotency);
                    effectValue = Mathf.Clamp(effectValue, 0.05f, 1.0f);

                    status = target.gameObject.GetOrAddComponent<HollowLifeEffect>();

                    if (protectionLvl > 0)
                    {
                        float tValue = Mathf.Pow(0.90f, protectionLvl);
                        float lerp = Mathf.Lerp(1.0f, effectValue, tValue);

                        status.ApplyTempHealthCap(lerp);
                        target.data.health *= lerp;
                    }
                    else
                    {
                        status.ApplyTempHealthCap(effectValue);
                        target.data.health *= effectValue;
                    }
                }
            }

            // map obliteration!
            if (!hitPLayer)
            {
                MapUtils.RPCA_DestroyMapObject(hit.transform.gameObject);
            }

            MapUtils.RPCA_DestroyMapObjectsAtArea(gameObject.transform.position, effectRadius);

            // VFX part
            GameObject VFX = Instantiate(VFXPrefab, gameObject.transform.position + new Vector3(0.0f, 0.0f, 100.0f), Quaternion.identity);
            VFX.transform.localScale = Vector3.one * effectRadius;
            VFX.name = "OrbLiterationImpactVFX_Copy";
            VFX.GetComponent<Canvas>().sortingLayerName = "MostFront";
            VFX.GetComponent<Canvas>().sortingOrder = 10000;
            VFX.AddComponent<RemoveAfterSeconds>().seconds = 1.55f;

            return HasToReturn.canContinue;
        }


    }

    public class ObliterationStatus : ReversibleEffect
    {
        private float healthScale = 1.0f;

        public override void OnAwake()
        {
            this.SetLivesToEffect(999);
        }

        public void CullMaxHealth(float percentage)
        {
            healthScale *= percentage;

            characterDataModifier.maxHealth_mult = healthScale;

            try
            {
                ApplyModifiers();
            }
            catch (Exception exception)
            {
                Miscs.LogWarn("[GearUp] ObliterationStatus: caught an exception!");
                Miscs.LogWarn(exception);
            }

        }
    }
}
