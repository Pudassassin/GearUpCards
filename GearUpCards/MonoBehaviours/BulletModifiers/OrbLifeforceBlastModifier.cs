using System.Collections.Generic;
using System.Collections;

using UnboundLib;
using UnityEngine;
using UnboundLib.GameModes;
using ModdingUtils.MonoBehaviours;

using GearUpCards.Utils;
using GearUpCards.Extensions;
using UnityEngine.UI;
using Photon.Pun;
using System.Linq;

namespace GearUpCards.MonoBehaviours
{
    internal class OrbLifeforceBlastModifier : RayHitEffect
    {
        private static GameObject vfxOrb = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_LifeforceBlast_Orb");
        private static GameObject vfxAOE = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_LifeforceBlast_AOE");

        private static float procTickTime = 0.2f;

        private ProjectileHit projectileHit;
        // MoveTransform moveTransform;
        private Player casterPlayer;
        private CharacterStatModifiers casterStats;

        private float healFlat, healPercent, drainFlat, drainPercent;
        private float effectRadius, effectDuration, healAmp, healHinder;

        internal GameObject orbObject;
        internal float proxyTimer = 0.0f;
        internal float procTimer = 0.0f;
        internal bool effectEnable = false;

        internal bool fullArcane = false;

        public void Setup()
        {
            projectileHit = transform.root.GetComponentInParent<ProjectileHit>();
            casterPlayer = projectileHit.ownPlayer;
            casterStats = casterPlayer.data.stats;
            // moveTransform = transform.root.GetComponentInChildren<MoveTransform>();

            // Orb Stats
            healFlat        = 30.0f + (15.0f * casterStats.GetGearData().glyphPotency);
            healPercent     = 0.05f + (0.025f * casterStats.GetGearData().glyphPotency);
            drainFlat       = 50.0f + (25.0f * casterStats.GetGearData().glyphPotency);
            drainPercent    = 0.10f + (0.05f * casterStats.GetGearData().glyphPotency);

            effectRadius    = 5.0f + (0.5f * casterStats.GetGearData().glyphInfluence);
            effectDuration  = 5.0f + (1.0f * casterStats.GetGearData().glyphTime);

            // value is the multiplier to be used multiplicatively
            healAmp = 1.5f + (0.15f * (float)casterStats.GetGearData().glyphPotency);
            healAmp = Mathf.Clamp(healAmp, 1.0f, 2.5f);

            healHinder = 0.4f - (0.1f * (float)casterStats.GetGearData().glyphPotency);
            healHinder = Mathf.Clamp(healHinder, 0.0f, 1.0f);

            if (casterPlayer.gameObject.GetComponent<CharacterStatModifiers>().GetGearData().arcaneConversionStack > 0)
            {
                fullArcane = true;
            }

            // visuals
            orbObject = Instantiate(vfxOrb, transform.root);
            orbObject.transform.localEulerAngles = new Vector3(270.0f, 180.0f, 0.0f);
            orbObject.transform.localPosition = Vector3.zero;
            orbObject.transform.localScale = Vector3.one;

            // GameObject aoeObject = Instantiate(vfxAOE, transform.root);
            // aoeObject.transform.localEulerAngles = new Vector3(270.0f, 180.0f, 0.0f);
            // aoeObject.transform.localPosition = Vector3.zero;
            // aoeObject.transform.localScale = Vector3.one * effectRadius;
        }

        public void Update()
        {
            if (effectEnable)
            {

            }
            else
            {
                MoveTransform moveTransform = GetComponentInParent<MoveTransform>();
                if (moveTransform != null)
                {
                    Setup();
                    effectEnable = true;
                }
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            float distance;
            foreach (Player target in PlayerManager.instance.players)
            {
                if (!target.gameObject.activeInHierarchy || target.data.healthHandler.isRespawning)
                {
                    // either dead or reviving
                    continue;
                }

                distance = (target.gameObject.transform.position - transform.root.position).magnitude;
                if (distance > effectRadius)
                {
                    // ...out of range
                    continue;
                }

                if (target.teamID == casterPlayer.teamID)
                {
                    // Heal friends
                    float healAmount = (healFlat + (target.data.maxHealth * healPercent));
                    target.data.healthHandler.Heal(healAmount);

                    LifeforceBlastStatus status = target.gameObject.GetOrAddComponent<LifeforceBlastStatus>();
                    status.ApplyEffect(healAmp, effectDuration, true);
                }
                else
                {
                    CharacterStatModifiers stats = target.gameObject.GetComponent<CharacterStatModifiers>();
                    int protectionLvl = stats.GetGearData().glyphProtection;

                    // drain enemies' lives
                    float drainAmount = (drainFlat + (target.data.maxHealth * drainPercent));

                    if (protectionLvl > 0)
                    {
                        drainAmount *= Mathf.Pow(0.85f, protectionLvl);
                    }

                    if (fullArcane)
                    {
                        target.data.healthHandler.Heal(-drainAmount);
                        target.data.healthHandler.RPCA_SendTakeDamage(new Vector2(0.05f, 0.0f), target.transform.position);
                    }
                    else
                    {
                        target.data.healthHandler.Heal(-drainAmount * 0.5f);
                        target.data.healthHandler.RPCA_SendTakeDamage(new Vector2(drainAmount * 0.5f, 0.0f), target.transform.position, playerID: casterPlayer.playerID);
                    }

                    LifeforceBlastStatus status = target.gameObject.GetOrAddComponent<LifeforceBlastStatus>();
                    if (protectionLvl > 0)
                    {
                        float tValue = Mathf.Pow(0.90f, protectionLvl);
                        float lerp = Mathf.Lerp(1.0f, healHinder, tValue);

                        status.ApplyEffect(lerp, effectDuration, false);
                    }
                    else
                    {
                        status.ApplyEffect(healHinder, effectDuration, false);
                    }
                }
            }

            // VFX part
            GameObject aoeObject = Instantiate(vfxAOE);
            // aoeObject.transform.localEulerAngles = new Vector3(270.0f, 180.0f, 0.0f);
            aoeObject.transform.position = hit.point;
            aoeObject.transform.localScale = Vector3.one * effectRadius;
            RemoveAfterSeconds remover = aoeObject.AddComponent<RemoveAfterSeconds>();
            remover.seconds = 2.5f;

            try
            {
                int clientTeamID = PlayerManager.instance.players.First(player => player.data.view.IsMine).teamID;
                if (projectileHit.ownPlayer.teamID == clientTeamID)
                {
                    aoeObject.transform.Find("Circle_Root/Circle_Thorns (1)").gameObject.SetActive(false);
                }
                else
                {
                    aoeObject.transform.Find("Circle_Root/Circle_Wrealth (1)").gameObject.SetActive(false);
                }
            }
            catch (System.Exception exception)
            {
                Miscs.LogWarn(exception);
            }

            return HasToReturn.canContinue;
        }
    }
}
