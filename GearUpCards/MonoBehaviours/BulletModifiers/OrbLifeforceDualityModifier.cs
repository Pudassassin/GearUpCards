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
    internal class OrbLifeforceDualityModifier : RayHitEffect
    {
        private static GameObject vfxOrb = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_LifeforceDuorbity_Orb");
        private static GameObject vfxAOE = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_LifeforceDuorbity_AOE");
        private static GameObject vfxBeamHeal = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_Beam_HealRay");
        private static GameObject vfxBeamDrain = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_Beam_DrainRay");

        private static float procTickTime = 0.2f;

        private ProjectileHit projectileHit;
        MoveTransform moveTransform;
        private Player casterPlayer;
        private CharacterStatModifiers casterStats;

        private float healFlatRate, healPercentRate, drainFlatRate, drainPercentRate;
        private float orbLifeTime, orbMaxDuration, proxyTimeLimit, effectRadius;
        private float proxySpeed;

        private Dictionary<Player, GameObject> beamPlayerPairs;
        private int proxyPlayerCount = 0;

        private List<Player> proxyPlayers = new List<Player>();

        internal float previousSpeed;
        internal bool isSlowedDown = false;

        internal GameObject orbObject;
        internal float proxyTimer = 0.0f;
        internal float procTimer = 0.0f;
        internal float orbAliveTime = 0.0f;
        internal bool effectEnable = false;

        internal bool fullArcane = false;

        public void Setup()
        {
            projectileHit = transform.root.GetComponentInParent<ProjectileHit>();
            casterPlayer = projectileHit.ownPlayer;
            casterStats = casterPlayer.data.stats;

            beamPlayerPairs = new Dictionary<Player, GameObject>();

            moveTransform = transform.root.GetComponentInChildren<MoveTransform>();
            // moveTransform.velocity = moveTransform.velocity.normalized * 20.0f;

            // Orb Stats
            healFlatRate        = 20.0f + (10.0f * casterStats.GetGearData().glyphPotency);
            healPercentRate     = 0.01f + (0.005f * casterStats.GetGearData().glyphPotency);
            drainFlatRate       = 25.0f + (12.5f * casterStats.GetGearData().glyphPotency);
            drainPercentRate    = 0.02f + (0.01f * casterStats.GetGearData().glyphPotency);

            orbLifeTime         = 7.5f + (1.5f * casterStats.GetGearData().glyphTime);
            orbMaxDuration      = orbLifeTime * 2.0f;
            proxyTimeLimit      = Mathf.Clamp(1.5f + (0.15f * (casterStats.GetGearData().glyphTime - casterStats.GetGearData().glyphMagickFragment)),
                                              0.3f,
                                              3.0f);
            effectRadius        = 8.0f + (0.5f * casterStats.GetGearData().glyphInfluence);

            proxySpeed = 7.5f;

            transform.root.GetComponent<RemoveAfterSeconds>().seconds = orbMaxDuration + 2.5f;
            procTimer = procTickTime;

            if (casterPlayer.gameObject.GetComponent<CharacterStatModifiers>().GetGearData().arcaneConversionStack > 0)
            {
                fullArcane = true;
            }

            // visuals
            orbObject = Instantiate(vfxOrb, transform.root);
            orbObject.transform.localEulerAngles = new Vector3(270.0f, 180.0f, 0.0f);
            orbObject.transform.localPosition = Vector3.zero;
            orbObject.transform.localScale = Vector3.one;

            GameObject aoeObject = Instantiate(vfxAOE, transform.root);
            aoeObject.transform.localEulerAngles = new Vector3(270.0f, 180.0f, 0.0f);
            aoeObject.transform.localPosition = Vector3.zero;
            aoeObject.transform.localScale = Vector3.one * effectRadius;

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

            GameObject tempGameObject;
            BeamVFXMono tempBeamMono;
            foreach (Player item in PlayerManager.instance.players)
            {
                if (item.teamID == casterPlayer.teamID)
                {
                    // friendly
                    tempGameObject = Instantiate(vfxBeamHeal, transform.root);
                }
                else
                {
                    // enemy
                    tempGameObject = Instantiate(vfxBeamDrain, transform.root);
                }

                tempGameObject.SetActive(false);
                tempBeamMono = tempGameObject.AddComponent<BeamVFXMono>();
                tempBeamMono.TLinkFrom = transform.root;
                tempBeamMono.TLinkTo = item.transform.root;

                beamPlayerPairs[item] = tempGameObject;
            }
        }

        public void Update()
        {
            if (effectEnable)
            {
                procTimer += TimeHandler.deltaTime;
                orbAliveTime += TimeHandler.deltaTime;

                if (orbAliveTime >= orbMaxDuration)
                {
                    PhotonNetwork.Destroy(transform.root.gameObject);
                }
                else if (orbLifeTime < 0.0f)
                {
                    PhotonNetwork.Destroy(transform.root.gameObject);
                }

                if (proxyPlayerCount <= 0)
                {
                    proxyTimer += TimeHandler.deltaTime;
                    if (proxyTimer > proxyTimeLimit)
                    {
                        orbLifeTime -= TimeHandler.deltaTime;

                        if (isSlowedDown)
                        {
                            moveTransform.velocity = moveTransform.velocity.normalized * previousSpeed;
                            isSlowedDown = false;
                        }
                    }
                }
                else
                {
                    proxyTimer = 0.0f;

                    if (!isSlowedDown)
                    {
                        previousSpeed = moveTransform.velocity.magnitude;
                        moveTransform.velocity = moveTransform.velocity.normalized * proxySpeed;

                        isSlowedDown = true;
                    }
                }

                if (procTimer >= procTickTime)
                {
                    // actual gameplay effects
                    bool linkFlag;
                    float distance;
                    Vector2 orbPosition = new Vector2(transform.root.position.x, transform.root.position.y);
                    proxyPlayerCount = 0;

                    foreach (Player target in beamPlayerPairs.Keys)
                    {
                        linkFlag = true;

                        if (!target.gameObject.activeInHierarchy || target.data.healthHandler.isRespawning)
                        {
                            // either dead or reviving, unlink it
                            linkFlag = false;
                        }

                        distance = (target.gameObject.transform.position - transform.root.position).magnitude;
                        if (distance > effectRadius)
                        {
                            // ...out of range
                            linkFlag = false;
                        }

                        if (PlayerManager.instance.CanSeePlayer(orbPosition, target).canSee == false)
                        {
                            // ...not in line of sight
                            linkFlag = false;
                        }

                        beamPlayerPairs[target].SetActive(linkFlag);

                        if (linkFlag)
                        {
                            proxyPlayerCount++;

                            if (target.teamID == casterPlayer.teamID)
                            {
                                // Heal friends
                                orbLifeTime -= procTickTime;
                                float healAmount = (healFlatRate + (target.data.maxHealth * healPercentRate)) * procTickTime;
                                target.data.healthHandler.Heal(healAmount);
                            }
                            else
                            {
                                CharacterStatModifiers stats = target.gameObject.GetComponent<CharacterStatModifiers>();
                                int protectionLvl = stats.GetGearData().glyphProtection;

                                // drain enemies' lives
                                orbLifeTime += procTickTime * 0.25f;
                                float drainAmount = (drainFlatRate + (target.data.maxHealth * drainPercentRate)) * procTickTime;

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
                            }
                        }
                    }

                    procTimer -= procTickTime;
                }

                orbObject.transform.localScale = Vector3.one * (0.5f + (1.5f * Mathf.Clamp01(orbLifeTime / orbMaxDuration)));

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
            return HasToReturn.canContinue;
        }
    }
}
