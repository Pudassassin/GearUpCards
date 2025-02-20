using System;
using System.Collections.Generic;
using UnityEngine;

using UnboundLib;
using ModdingUtils.Utils;

using GearUpCards.Extensions;
using GearUpCards.Utils;
using GearUpCards.Cards;
using static GearUpCards.Utils.Miscs;
using System.Net.Http.Headers;

namespace GearUpCards.MonoBehaviours
{
    public class MysticMissileModifier : RayHitEffect
    {
        public static float ecoScaling = 0.75f;

        private static float seekAngleBase = 120.0f;
        private static float seekAngleScaling = 30.0f;

        private static float turnSpeedBase = 210.0f;
        private static float turnSpeedScaling = 30.0f;

        private static float accelBase = 40.0f;
        private static float accelScaling = 10.0f;
        // default speed is 50
        private static float homingSpeedBase = 40.0f;
        private static float homingSpeedScaling = 10.0f;

        private static float homingRangeBase = 30.0f;
        private static float homingRangeScaling = 5.0f;

        private static float explosionForceBase = 4000.0f;
        private static float explosionForceScaling = 2000.0f;

        private static float explosionRadiusBase = 4.5f;
        private static float explosionRadiusScaling = 1.5f;

        private static float damageFactorBase = 0.45f;
        private static float damageFactorScaling = 0.15f;

        private static float bouncePowerMulBase = 0.60f;
        private static float bouncePowerMulScaling = 0.05f;

        private static float powerLevelMin = 0.10f;

        private static float VFXLogBase = 5.0f;
        private static float VFXMinSize = 0.5f;
        private static float VFXMaxSize = 25.0f;
        private static float VFXTrailScale = 0.75f;
        private static float procTime = 0.05f;

        private MoveTransform bulletMove;
        private RayHitReflect rayHitReflect;
        private ProjectileHit projectileHit;
        private Explosion explosionImpact;
        private SyncBulletPosition syncMono;
        private Miscs.SetColorToParticles partColor;
        private Miscs.SetColorToParticles partColorExp;

        private ArcaneConversionModifier arcaneMod;

        private Player shooterPlayer;
        private Gun shooterGun;
        private CharacterStatModifiers shooterStats;

        // card stack counts: extra copies boost overall stats
        private int stackCount;
        // make it bounce more before losing power
        private int glyphGeometric;
        // area of effect!
        private int glyphInfluence;
        // adjust explosion/magic blast damage ratio, make explosion more forceful!
        private int glyphPotency;
        // make it home toward enemies
        private int glyphDivination;


        // internals
        private List<Player> enemyPlayers = new List<Player>();

        private Player homingTarget = null;
        private bool seeEnemy = false;
        private float targetEnemyDistance;

        private float seekAngle = 0.0f;
        private float turnSpeed = 0.0f;
        private float acceleration = 0.0f;
        private float homingSpeed = 0.0f;
        private float homingRange = 0.0f;

        private float bouncePowerFactor = 0.5f;
        private float currentPower = 1.0f;

        private float prevGravity;
        private float tickTimer = 0.0f;

        private int bounceCount = 0;
        private int bounceFromPlayer = 0;
        private bool dieNextHit = false;

        private float coreDamageSum;

        // temps
        Vector3 moveVelocity, vecToTarget;

        bool effectEnable = false;

        public void Update()
        {
            if (effectEnable)
            {
                // gameObject.transform.localScale = Vector3.one * Mathf.Clamp(Mathf.Log(projectileHit.damage * projectileHit.dealDamageMultiplierr, logBaseProj), 1.0f, 15.0f);
                // foreach (Transform child in transform)
                // {
                //     // child.transform.localScale = Vector3.one * Mathf.Clamp(Mathf.Log(projectileHit.damage * projectileHit.dealDamageMultiplierr, logBaseProj), 1.0f, 15.0f);
                //     child.transform.localScale = Vector3.one * Mathf.Clamp(projectileHit.damage * projectileHit.dealDamageMultiplierr / dmgDiv, 0.25f, 25.0f);
                // }

                // transform.GetChild(0).transform.localScale = Vector3.one * Mathf.Clamp(projectileHit.damage * projectileHit.dealDamageMultiplierr / dmgDiv, 0.5f, 25.0f);
                // transform.GetChild(1).transform.localScale = Vector3.one * Mathf.Clamp(projectileHit.damage * projectileHit.dealDamageMultiplierr / dmgDiv, 1.0f, 25.0f) * trailScale;
                
                // scale bullet effect
                coreDamageSum = projectileHit.damage;
                if (arcaneMod != null)
                {
                    coreDamageSum += arcaneMod.arcaneDamage;
                }
                float scale = 1.0f + Mathf.Log(coreDamageSum / 55.0f, VFXLogBase);
                scale = Mathf.Clamp(scale, VFXMinSize, VFXMaxSize);

                transform.GetChild(0).transform.localScale = Vector3.one * scale;
                transform.GetChild(1).transform.localScale = Vector3.one * scale * VFXTrailScale;

                // update color and VFX radius
                if (GearUpCards.EcoModeVFX)
                {
                    // Miscs.Log("\n\n[GearUp] : MysticMissileMod - DoHitEffect() - Eco Update");
                    MysticMissileCard.objectSpawnDict[shooterPlayer.playerID].effect.transform.localScale = Vector3.one * explosionImpact.range * ecoScaling;

                    GameObject VFXObject = MysticMissileCard.objectSpawnDict[shooterPlayer.playerID].effect;
                    GameObject AOEObject = Miscs.GetChildByHierachy(VFXObject, "MagicExplosion_Root\\AoECircle");
                    SpriteRenderer renderer = AOEObject.GetComponent<SpriteRenderer>();
                    renderer.color = shooterGun.projectileColor;
                    renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 0.10f);

                    AOEObject = Miscs.GetChildByHierachy(VFXObject, "MagicExplosion_Root\\ShockCircle");
                    renderer = AOEObject.GetComponent<SpriteRenderer>();
                    renderer.color = shooterGun.projectileColor;
                    renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1.0f);
                }
                else
                {
                    MysticMissileCard.objectSpawnDict[shooterPlayer.playerID].effect.transform.localScale = Vector3.one * explosionImpact.range * 0.25f;

                    partColorExp.targetColor = shooterGun.projectileColor;
                }

                partColor.targetColor = shooterGun.projectileColor;

                // homing and power level update
                if (tickTimer >= procTime)
                {
                    // subject to change the glyph
                    if (glyphDivination > 0)
                    {
                        moveVelocity = bulletMove.velocity;
                        moveVelocity.z = 0;

                        // home to current enemy
                        if (seeEnemy)
                        {
                            if (PlayerManager.instance.CanSeePlayer(transform.root.position, homingTarget).canSee)
                            {
                                vecToTarget = homingTarget.transform.position - transform.root.position;
                                vecToTarget.z = 0;

                                if (Vector3.Angle(moveVelocity, vecToTarget) <= seekAngle / 2.0f)
                                {
                                    // rotate bullet to target
                                    Vector3 newDirection = Vector3.RotateTowards(moveVelocity.normalized, vecToTarget.normalized, turnSpeed * Mathf.Deg2Rad * procTime, 0.0f);
                                    float speedDelta = acceleration * procTime;
                                    if (Mathf.Abs(moveVelocity.magnitude - homingSpeed) <= speedDelta)
                                    {
                                        bulletMove.velocity = newDirection * homingSpeed;
                                    }
                                    else if (moveVelocity.magnitude < homingSpeed)
                                    {
                                        bulletMove.velocity = (moveVelocity.magnitude + speedDelta) * newDirection;
                                    }
                                    else
                                    {
                                        bulletMove.velocity = (moveVelocity.magnitude - speedDelta) * newDirection;
                                    }
                                }
                                else
                                {
                                    bulletMove.gravity = prevGravity;
                                    seeEnemy = false;
                                    homingTarget = null;
                                }
                            }
                            else
                            {
                                bulletMove.gravity = prevGravity;
                                seeEnemy = false;
                                homingTarget = null;
                            }
                        }

                        // scan for new enemy
                        if (!seeEnemy)
                        {
                            foreach (Player enemy in enemyPlayers)
                            {
                                // skip dead enemy player
                                if (!PlayerStatus.PlayerAliveAndSimulated(enemy) || enemy.data.healthHandler.isRespawning)
                                {
                                    continue;
                                }
                                // player in line of sight
                                else if (PlayerManager.instance.CanSeePlayer(transform.root.position, enemy).canSee)
                                {
                                    vecToTarget = enemy.transform.position - transform.root.position;
                                    vecToTarget.z = 0;

                                    // check against seek angle and range
                                    if (Vector3.Angle(moveVelocity, vecToTarget) <= seekAngle / 2.0f && vecToTarget.magnitude <= homingRange)
                                    {
                                        // set first valid target
                                        if (!seeEnemy)
                                        {
                                            prevGravity = bulletMove.gravity;
                                            bulletMove.gravity = 0.005f;

                                            homingTarget = enemy;
                                            seeEnemy = true;
                                            targetEnemyDistance = vecToTarget.magnitude;
                                        }
                                        // set closer enemy
                                        else if (vecToTarget.magnitude < targetEnemyDistance)
                                        {
                                            homingTarget = enemy;
                                            targetEnemyDistance = vecToTarget.magnitude;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (bounceCount > glyphGeometric)
                    {
                        currentPower = Mathf.Pow(bouncePowerFactor, bounceCount - glyphGeometric);
                        // if (currentPower < powerLevelMin)
                        // {
                        //     dieNextHit = true;
                        // }
                    }
                    else
                    {
                        currentPower = 1.0f;
                    }

                    tickTimer -= procTime;
                }

                tickTimer += TimeHandler.deltaTime;
            }
            else
            {
                MoveTransform moveTransform = GetComponentInParent<MoveTransform>();
                if (moveTransform != null)
                {
                    try
                    {
                        this.ExecuteAfterFrames(1, () => 
                        {
                            Setup();
                            effectEnable = true;
                        });
                    }
                    catch (Exception exception)
                    {
                        Miscs.LogWarn(exception);

                        this.enabled = false;
                        return;
                    }
                }
            }
        }

        public void Setup()
        {
            SimpleRemoveAfterUnparent remover = gameObject.GetComponent<SimpleRemoveAfterUnparent>();
            remover.parent = gameObject.transform.parent.gameObject;
            remover.enabled = true;

            // Miscs.Log("[GearUpCard] Mystic Missle: Setup()");
            projectileHit = gameObject.GetComponentInParent<ProjectileHit>();
            bulletMove = gameObject.GetComponentInParent<MoveTransform>();
            explosionImpact = gameObject.GetComponent<Explosion>();
            arcaneMod = transform.root.gameObject.GetComponentInChildren<ArcaneConversionModifier>();

            // fetch player stats
            // Miscs.Log("[GearUpCard] Mystic Missle: Setup() - fetch player stats");
            shooterPlayer = projectileHit.ownPlayer;
            shooterGun = projectileHit.ownWeapon.GetComponent<Gun>();
            shooterStats = shooterPlayer.gameObject.GetComponent<CharacterStatModifiers>();

            stackCount = shooterStats.GetGearData().mysticMissileStack;
            glyphDivination = shooterStats.GetGearData().glyphDivination;
            glyphGeometric = shooterStats.GetGearData().glyphGeometric;
            glyphInfluence = shooterStats.GetGearData().glyphInfluence;
            glyphPotency = shooterStats.GetGearData().glyphPotency;

            // homing, angle in degrees
            float projSimSpeed = Mathf.Clamp(shooterGun.projectielSimulatonSpeed, 0.2f, 5.0f);

            turnSpeed = turnSpeedBase + (glyphDivination + stackCount - 1) * turnSpeedScaling;
            turnSpeed *= projSimSpeed;

            acceleration = accelBase + (glyphDivination + stackCount - 1) * accelScaling;
            acceleration *= projSimSpeed;

            seekAngle = seekAngleBase + (glyphDivination + stackCount - 1) * seekAngleScaling;
            homingSpeed = homingSpeedBase + glyphDivination * homingSpeedScaling;
            homingRange = homingRangeBase + (glyphDivination + (stackCount - 1) * 2) * homingRangeScaling;

            // power scale on bounce
            bouncePowerFactor = bouncePowerMulBase + (bouncePowerMulScaling * glyphPotency);

            // declare enemies
            // Miscs.Log("[GearUpCard] Mystic Missle: Setup() - declare enemies");
            foreach (Player player in PlayerManager.instance.players)
            {
                if (player.teamID != shooterPlayer.teamID)
                {
                    enemyPlayers.Add(player);
                }
            }

            // setup RayHitReflect
            // Miscs.Log("[GearUpCard] Mystic Missle: Setup() - setup RayHitReflect");
            rayHitReflect = transform.root.gameObject.GetComponent<RayHitReflect>();
            if (rayHitReflect == null)
            {
                rayHitReflect = transform.root.gameObject.AddComponent<RayHitReflect>();
                rayHitReflect.reflects = 1;
                rayHitReflect.speedM = 1.0f;
                rayHitReflect.dmgM = 1.0f;
                projectileHit.effects.Insert(0, rayHitReflect);
            }
            else
            {
                rayHitReflect.reflects += 1;
            }

            // setup Explosion impact script
            coreDamageSum = projectileHit.damage;
            if (arcaneMod != null)
            {
                coreDamageSum += arcaneMod.arcaneDamage;
            }

            //Miscs.Log("[GearUpCard] Mystic Missle: Setup() - setup Explosion impact script");
            explosionImpact = MysticMissileCard.objectSpawnDict[shooterPlayer.playerID].effect.GetComponent<Explosion>();

            explosionImpact.auto = true;
            //explosionImpact.ignoreWalls = true;
            explosionImpact.force = explosionForceBase + explosionForceScaling * ((stackCount - 1) * 2 + glyphPotency);
            explosionImpact.range = explosionRadiusBase + explosionRadiusScaling * ((stackCount - 1) * 2 + glyphInfluence);
            //explosionImpact.damage = projectileHit.damage * (damageFactorBase + damageFactorScaling * ((stackCount - 1) * 2 + glyphPotency));
            explosionImpact.damage = coreDamageSum * projectileHit.dealDamageMultiplierr * (damageFactorBase + damageFactorScaling * ((stackCount - 1) * 2 + glyphPotency));

            explosionImpact.objectForceMultiplier = 3.0f;
            explosionImpact.scaleSlow = false;
            explosionImpact.scaleSilence = false;
            explosionImpact.scaleDmg = false;
            explosionImpact.scaleRadius = false;
            explosionImpact.scaleStun = false;
            explosionImpact.scaleForce = false;

            syncMono = transform.root.gameObject.GetOrAddComponent<SyncBulletPosition>();
            syncMono.interval = 0.2f;

            partColor = gameObject.GetOrAddComponent<Miscs.SetColorToParticles>();
            partColor.targetColor = shooterGun.projectileColor;

            if (GearUpCards.EcoModeVFX)
            {
                GameObject VFXObject = MysticMissileCard.objectSpawnDict[shooterPlayer.playerID].effect;
                GameObject AOEObject = Miscs.GetChildByHierachy(VFXObject, "MagicExplosion_Root\\AoECircle");
                SpriteRenderer renderer = AOEObject.GetComponent<SpriteRenderer>();
                renderer.color = shooterGun.projectileColor + new Color(0.0f, 0.0f, 0.0f, 0.15f);
            }
            else
            {
                partColorExp = MysticMissileCard.objectSpawnDict[shooterPlayer.playerID].effect.GetComponent<Miscs.SetColorToParticles>();
                partColorExp.targetColor = shooterGun.projectileColor;
            }


            // make this resolve first
            projectileHit.effects.Remove(this);
            projectileHit.effects.Insert(0, this);
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            // Miscs.Log("\n\n[GearUp] : MysticMissileMod - DoHitEffect() - A");

            // bool explode = true;
            if (hit.transform == null)
            {
                // hitting screen edge

                // explode = false;
                bounceCount--;
            }
            else
            {
                // hitting other bullet
                if (hit.transform.gameObject.tag.Contains("Bullet"))
                {
                    // give bounce buffer
                    rayHitReflect.reflects++;

                }

                // hitting map or player

                if (hit.transform.GetComponent<Player>())
                {
                    // deal direct hit magic damage

                    // give bounce buffer, or exhaust it quicker
                    if (bounceFromPlayer <= glyphGeometric)
                    {
                        rayHitReflect.reflects++;
                    }
                    else
                    {
                        rayHitReflect.reflects--;
                    }
                    bounceFromPlayer++;
                }
            }

            // Miscs.Log("\n\n[GearUp] : MysticMissileMod - DoHitEffect() - B");
            // hit.point += hit.normal * 0.2f;
            transform.position = (Vector3)hit.point + (Vector3)hit.normal * 0.2f;
            syncMono.CallSyncs();

            //Miscs.Log("\n\n[GearUp] : MysticMissileMod - DoHitEffect() - C");
            // update power, explode and deal area magic damage
            explosionImpact.damage = coreDamageSum * projectileHit.dealDamageMultiplierr * (damageFactorBase + damageFactorScaling * ((stackCount - 1) * 2 + glyphPotency));
            explosionImpact.damage *= Mathf.Clamp(currentPower, powerLevelMin, 2.0f);

            explosionImpact.force = explosionForceBase + explosionForceScaling * ((stackCount - 1) * 2 + glyphPotency);
            explosionImpact.force *= Mathf.Clamp(currentPower, powerLevelMin, 2.0f);

            // power loss on bounce
            // if (bounceCount > glyphGeometric && hit.transform != null)
            // {
            //     projectileHit.
            // }

            // Miscs.Log("\n\n[GearUp] : MysticMissileMod - DoHitEffect() - D");
            bounceCount++;

            if (rayHitReflect.reflects <= 1 || dieNextHit)
            {
                rayHitReflect.reflects = 0;
            }
            return HasToReturn.canContinue;
        }

        public void Destroy()
        {
            Destroy(this);
        }

    }
}
