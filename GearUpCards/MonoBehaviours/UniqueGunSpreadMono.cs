using System.Collections.Generic;
using System.Collections;
using System;
using System.Reflection;

using UnboundLib;
using UnityEngine;
using System.Linq;
using UnboundLib.GameModes;
using Photon.Pun;
using ModdingUtils.MonoBehaviours;
using HarmonyLib;

using GearUpCards.Extensions;
using GearUpCards.Utils;
using GearUpCards.Cards;

namespace GearUpCards.MonoBehaviours
{
    internal class UniqueGunSpreadMono : MonoBehaviour
    {
        private const float procTime = 0.1f;
        public static int flakProjectileAdd = 1;

        public static GameObject objectToSpawnParallel = null;
        public static GameObject objectToSpawnFlak = null;

        internal Player player;
        internal Gun gun;
        internal CharacterStatModifiers stats;

        internal float prevSpread;
        internal float prevSpreadMul;
        internal float prevEvenSpread;
        internal float prevGravity;
        internal float prevBurstDelay;
        internal float prevAttackCooldown;

        internal float timer = 0.0f;

        internal bool effectApplied = false;
        internal bool wasDeactivated = false;

        internal Action<BlockTrigger.BlockTriggerType> blockAction;
        internal Action attackAction;
        internal Action<GameObject> shootAction;

        private GameObject oldGunObject, newGunObject, dummyGunObject0, dummyGunObject1;
        private GameObject gameObjectToAdd;
        private Gun playerOldGun, newSpreadGun;
        public Gun dummySpreadGun0 = null;
        public Gun dummySpreadGun1 = null;

        internal bool isGunReplaced = false;
        internal GearUpConstants.ModType lastGunSpreadMod = GearUpConstants.ModType.none;
        internal GameObject bulletLifetimeFixer;

        internal int bulletFiredIndex = 0;
        internal float parallelWidth = 0.0f;

        // Flak Cannon Stat Clamps
        private const float FlakCannon_minAttackTime = 0.15f;
        private const float FlakCannon_maxAttackSpeedMul = 1.5f;

        public float statsDelta_FlakCannon_AttackTime_ADD = 0.0f;
        public float statsDelta_FlakCannon_AttackSpeedMul_MUL = 1.0f;

        public void Awake()
        {
            this.player = this.gameObject.GetComponent<Player>();
            this.gun = this.gameObject.GetComponent<WeaponHandler>().gun;
            this.stats = this.gameObject.GetComponent<CharacterStatModifiers>();

            bulletLifetimeFixer = new GameObject("BulletLifetimeFixer", typeof(BulletLifetimeFix));

            oldGunObject = new GameObject("UniqueGunSpreadHolder_oldGun");
            oldGunObject.transform.parent = player.transform;
            oldGunObject.transform.localPosition = Vector3.zero;

            newGunObject = new GameObject("UniqueGunSpreadHolder_newGun");
            newGunObject.transform.parent = player.transform;
            newGunObject.transform.localPosition = Vector3.zero;

            dummyGunObject0 = new GameObject("UniqueGunSpreadHolder_dummyGun0");
            dummyGunObject0.transform.parent = player.transform;
            dummyGunObject0.transform.localPosition = Vector3.zero;

            dummyGunObject1 = new GameObject("UniqueGunSpreadHolder_dummyGun1");
            dummyGunObject1.transform.parent = player.transform;
            dummyGunObject1.transform.localPosition = Vector3.zero;

            SetupDummyGun();

            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);

            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnRematch);
        }

        public void Start()
        {
            // this.playerOldGun = new Gun();

            this.attackAction = new Action(this.GetDoAttackAction(this.player, this.gun));
            // this.gun.AddAttackAction(this.attackAction);

            this.shootAction = new Action<GameObject>(this.GetDoShootAction(this.player, this.gun));
            // this.gun.ShootPojectileAction = (Action<GameObject>)Delegate.Combine(this.gun.ShootPojectileAction, this.shootAction);
        }

        public void Update()
        {
            // Respawn case
            if (wasDeactivated)
            {
                ApplyModifier();
            
                wasDeactivated = false;
                effectApplied = true;
            }

            if (effectApplied && stats.GetGearData().gunSpreadMod == GearUpConstants.ModType.gunSpreadFlak)
            {
                // hard-disabling gun burst
                gun.timeBetweenBullets = 0.5f;
                gun.bursts = 0;

                // hard-capping firerate - cooldown
                if (gun.attackSpeed < FlakCannon_minAttackTime)
                {
                    statsDelta_FlakCannon_AttackTime_ADD += FlakCannon_minAttackTime - gun.attackSpeed;
                    gun.attackSpeed = FlakCannon_minAttackTime;
                }
                else if (gun.attackSpeed > FlakCannon_minAttackTime && statsDelta_FlakCannon_AttackTime_ADD > 0.0f)
                {
                    float tempTime = gun.attackSpeed - statsDelta_FlakCannon_AttackTime_ADD;
                    statsDelta_FlakCannon_AttackTime_ADD = 0.0f;

                    if (tempTime < FlakCannon_minAttackTime)
                    {
                        statsDelta_FlakCannon_AttackTime_ADD += FlakCannon_minAttackTime - tempTime;
                        gun.attackSpeed = FlakCannon_minAttackTime;
                    }
                    else
                    {
                        gun.attackSpeed = tempTime;
                    }
                }

                // hard-capping firerate - rate multiplier
                if (gun.attackSpeedMultiplier > FlakCannon_maxAttackSpeedMul)
                {
                    statsDelta_FlakCannon_AttackSpeedMul_MUL *= FlakCannon_maxAttackSpeedMul / gun.attackSpeedMultiplier;
                    gun.attackSpeedMultiplier = FlakCannon_maxAttackSpeedMul;
                }
                else if (gun.attackSpeedMultiplier < FlakCannon_maxAttackSpeedMul && statsDelta_FlakCannon_AttackSpeedMul_MUL < 1.0f)
                {
                    float tempMul = gun.attackSpeedMultiplier / statsDelta_FlakCannon_AttackSpeedMul_MUL;
                    statsDelta_FlakCannon_AttackSpeedMul_MUL = 1.0f;

                    if (tempMul > FlakCannon_maxAttackSpeedMul)
                    {
                        statsDelta_FlakCannon_AttackSpeedMul_MUL *= FlakCannon_maxAttackSpeedMul / tempMul;
                        gun.attackSpeedMultiplier = FlakCannon_maxAttackSpeedMul;
                    }
                    else
                    {
                        gun.attackSpeedMultiplier = tempMul;
                    }
                }
            }

        }

        private IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            prevSpread = gun.spread;
            prevEvenSpread = gun.evenSpread;
            prevSpreadMul = gun.multiplySpread;
            prevGravity = gun.gravity;
            prevBurstDelay = gun.timeBetweenBullets;
            prevAttackCooldown = gun.attackSpeed;

            SetupDummyGun();

            // if (stats.GetGearData().gunSpreadMod == GearUpConstants.ModType.gunSpreadFlak)
            // {
            //     Miscs.CopyGunStats(gun, playerOldGun);
            // }

            yield break;
        }

        private IEnumerator OnPointStart(IGameModeHandler gm)
        {
            ApplyModifier();

            wasDeactivated = false;
            effectApplied = true;

            yield break;
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            RemoveModifier();

            effectApplied = false;
            wasDeactivated = false;

            yield break;
        }

        private IEnumerator OnRematch(IGameModeHandler gm)
        {
            Destroy(this);
            yield break;
        }

        public void OnDisable()
        {
            bool isRespawning = player.data.healthHandler.isRespawning;
            // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting [{isRespawning}]");

            if (isRespawning)
            {
                // does nothing
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting!?");
            }
            else
            {
                RemoveModifier();

                wasDeactivated = true;
                effectApplied = false;
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - dead ded!?");
            }
        }

        public void OnDestroy()
        {
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);
            GameModeManager.RemoveHook(GameModeHooks.HookRoundStart, OnRoundStart);

            this.gun.ShootPojectileAction = (Action<GameObject>)Delegate.Remove(this.gun.ShootPojectileAction, this.shootAction);

            GameModeManager.RemoveHook(GameModeHooks.HookGameStart, OnRematch);
        }

        public Action GetDoAttackAction(Player player, Gun gun)
        {
            // trigger on firing one burst of bullets...?
            return delegate ()
            {
                try
                {
                    switch (stats.GetGearData().gunSpreadMod)
                    {
                        case GearUpConstants.ModType.gunSpreadParallel:
                            bulletFiredIndex = 0;
                            break;
                        case GearUpConstants.ModType.gunSpreadArc:
                            bulletFiredIndex = 0;
                            break;
                        case GearUpConstants.ModType.gunSpreadFlak:
                            bulletFiredIndex = 0;
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError($"[UniqueGunSpreadMono] AttackAction failed! [{player.playerID}]");
                    UnityEngine.Debug.LogWarning(exception);
                }
            };
        }

        public Action<GameObject> GetDoShootAction(Player player, Gun gun)
        {
            // triggers on firing EACH bullet
            return delegate (GameObject bulletFired)
            {
                try
                {
                    switch (stats.GetGearData().gunSpreadMod)
                    {
                        case GearUpConstants.ModType.gunSpreadArc:
                            ArcBulletModifier arc = bulletFired.GetOrAddComponent<ArcBulletModifier>();
                            // Miscs.Log(arc);

                            arc.arcSpread = prevSpread * prevSpreadMul;
                            if (arc.arcSpread * 360f < gun.numberOfProjectiles)
                            {
                                arc.arcSpread = ((float)gun.numberOfProjectiles) / 360f;
                            }

                            arc.bulletIndex = bulletFiredIndex;
                            arc.bulletsInVolley = gun.numberOfProjectiles;
                            bulletFiredIndex++;
                            bulletFiredIndex %= gun.numberOfProjectiles;

                            // Miscs.Log(bulletFiredIndex);
                            break;

                        case GearUpConstants.ModType.gunSpreadParallel:
                            ParallelBulletModifier parallel = bulletFired.GetOrAddComponent<ParallelBulletModifier>();
                            // Miscs.Log(parallel);

                            parallel.bulletIndex = bulletFiredIndex;
                            parallel.bulletsInVolley = gun.numberOfProjectiles;
                            parallel.parallelWidth = parallelWidth;
                            bulletFiredIndex++;
                            bulletFiredIndex %= gun.numberOfProjectiles;

                            // Miscs.Log(bulletFiredIndex);
                            break;

                        case GearUpConstants.ModType.gunSpreadFlak:
                            bulletFired.GetOrAddComponent<FlakShellModifier>();
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError($"[UniqueGunSpreadMono] ShootAction failed! [{player.playerID}]");
                    UnityEngine.Debug.LogWarning(exception);
                }
            };
        }

        private void SetupDummyGun()
        {
            // full backup of player's original gun
            playerOldGun = oldGunObject.GetOrAddComponent<Gun>();
            Miscs.CopyGunStats(gun, playerOldGun);

            // prepare player's replacement gun
            newSpreadGun = newGunObject.GetOrAddComponent<Gun>();
            dummySpreadGun0 = dummyGunObject0.GetOrAddComponent<Gun>();
            dummySpreadGun1 = dummyGunObject1.GetOrAddComponent<Gun>();
        }

        private void ResetEffect()
        {
            if (ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CardUtils.GearCategory.typeUniqueGunSpread))
            {
                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(CardUtils.GearCategory.typeUniqueGunSpread);
            }

            if (isGunReplaced)
            {
                Miscs.CopyGunStats(playerOldGun, this.gun);
                isGunReplaced = false;
            }

            Action action = Traverse.Create(gun).Field("attackAction").GetValue<Action>();
            action = (Action)Delegate.RemoveAll(action, attackAction);
            Traverse.Create(gun).Field("attackAction").SetValue((Action)action);

            gun.ShootPojectileAction = (Action<GameObject>)Delegate.RemoveAll(gun.ShootPojectileAction, shootAction);
        }

        private void RemoveModifier()
        {
            Miscs.Log("[GearUP] UniqueGunSpread: called remove");

            if (effectApplied)
            {
                if (isGunReplaced)
                {
                    Miscs.CopyGunStats(playerOldGun, gun);
                    isGunReplaced = false;
                }

                gun.spread = prevSpread;
                gun.evenSpread = prevEvenSpread;
                gun.multiplySpread = prevSpreadMul;
                gun.gravity = prevGravity;

                gun.attackSpeed = prevAttackCooldown;
                gun.timeBetweenBullets = prevBurstDelay;

                Action action = Traverse.Create(gun).Field("attackAction").GetValue<Action>();
                action = (Action)Delegate.RemoveAll(action, attackAction);
                Traverse.Create(gun).Field("attackAction").SetValue((Action)action);

                gun.ShootPojectileAction = (Action<GameObject>)Delegate.RemoveAll(gun.ShootPojectileAction, shootAction);

                Miscs.Log("[GearUP] UniqueGunSpread: removed");
            }
        }

        private void ApplyModifier()
        {
            Miscs.Log("[GearUP] UniqueGunSpread: called apply");

            if (!effectApplied)
            {
                // Miscs.CopyGunStats(gun, playerOldGun);

                switch (stats.GetGearData().gunSpreadMod)
                {
                    case GearUpConstants.ModType.gunSpreadArc:
                        ApplySpreadArc();
                        break;
                    case GearUpConstants.ModType.gunSpreadLine:
                        break;
                    case GearUpConstants.ModType.gunSpreadParallel:
                        ApplySpreadParallel();
                        break;
                    case GearUpConstants.ModType.gunSpreadFlak:
                        ApplySpreadFlak();
                        break;
                    case GearUpConstants.ModType.none:
                        ResetEffect();
                        break;
                    default:
                        break;
                }

                Miscs.Log("[GearUP] UniqueGunSpread: applied");
            }
        }

        private void ApplySpreadArc()
        {
            gun.spread = 0.0f;
            gun.evenSpread = 0.0f;
            gun.multiplySpread = 1.0f;

            FixBurstAttack();

            gun.AddAttackAction(this.attackAction);
            gun.ShootPojectileAction = (Action<GameObject>)Delegate.Combine(gun.ShootPojectileAction, this.shootAction);
        }

        private void ApplySpreadParallel()
        {
            // Width is the distance of outermost bullets
            parallelWidth = gun.spread * (2.5f + (0.10f * gun.numberOfProjectiles));
            parallelWidth *= gun.multiplySpread;
            parallelWidth = Mathf.Max(parallelWidth, 1.0f);

            gun.spread = 0.0f;
            gun.evenSpread = 0.0f;
            gun.multiplySpread = 1.0f;

            FixBurstAttack();

            gun.AddAttackAction(this.attackAction);
            gun.ShootPojectileAction = (Action<GameObject>)Delegate.Combine(gun.ShootPojectileAction, this.shootAction);
        }

        private void ApplySpreadFlak()
        {
            // flak cannon primary gun stats
            //Miscs.Log("ApplySpreadFlak()");
            Miscs.CopyGunStats(playerOldGun, newSpreadGun);

            newSpreadGun.attackID = player.playerID;

            newSpreadGun.bursts = 0;
            newSpreadGun.timeBetweenBullets = 0.5f;

            newSpreadGun.attackSpeed = 0.35f + Mathf.Clamp(playerOldGun.attackSpeed * 1.35f, 0.0f, playerOldGun.attackSpeed);
            newSpreadGun.attackSpeedMultiplier = 0.1f + Mathf.Clamp(playerOldGun.attackSpeedMultiplier, 0.0f, 1.5f);

            newSpreadGun.numberOfProjectiles = 1;

            newSpreadGun.damage = playerOldGun.damage * 0.80f;
            newSpreadGun.bulletDamageMultiplier = playerOldGun.bulletDamageMultiplier * 0.80f;

            newSpreadGun.damageAfterDistanceMultiplier = 1.0f;
            newSpreadGun.dmgMOnBounce = 1.0f;
            newSpreadGun.percentageDamage = 0.0f;

            newSpreadGun.spread = playerOldGun.spread * 0.5f;
            newSpreadGun.evenSpread = playerOldGun.evenSpread * 0.5f;
            newSpreadGun.multiplySpread = playerOldGun.multiplySpread * 0.5f;

            newSpreadGun.projectileSpeed = Mathf.Clamp(playerOldGun.projectileSpeed * 1.25f, 0.25f, 7.5f);
            newSpreadGun.projectielSimulatonSpeed = Mathf.Clamp(playerOldGun.projectielSimulatonSpeed, 0.05f, 5.0f);
            // newSpreadGun.drag = 0.0f;
            // newSpreadGun.dragMinSpeed = 1.0f;

            newSpreadGun.reflects = 24;


            newSpreadGun.knockback = playerOldGun.knockback * 1.5f;
            newSpreadGun.recoil = playerOldGun.recoil * 0.05f;

            newSpreadGun.destroyBulletAfter = 5.0f;

            //******//
            //fragmentation stats (Big sharpnel: has effects, less projectile)
            //******//

            //Miscs.Log("ApplySpreadFlak() : dummySpreadGun");
            Miscs.CopyGunStatsNoActions(playerOldGun, dummySpreadGun0);

            dummySpreadGun0.attackID = player.playerID;

            dummySpreadGun0.bursts = 0;
            dummySpreadGun0.timeBetweenBullets = 0.15f;

            // careful with this one!!
            dummySpreadGun0.numberOfProjectiles = 5 + Mathf.RoundToInt(Mathf.Log(playerOldGun.numberOfProjectiles, 2));

            dummySpreadGun0.damage = playerOldGun.damage * 0.60f;
            dummySpreadGun0.bulletDamageMultiplier = playerOldGun.bulletDamageMultiplier * 0.60f;
            dummySpreadGun0.percentageDamage = playerOldGun.percentageDamage * 0.2f;

            dummySpreadGun0.projectileSpeed = Mathf.Clamp(playerOldGun.projectileSpeed, 0.75f, 25.0f);
            dummySpreadGun0.projectielSimulatonSpeed = Mathf.Clamp(playerOldGun.projectielSimulatonSpeed, 0.20f, 10.0f);

            dummySpreadGun0.evenSpread = 0.0f;
            dummySpreadGun0.spread = 0.65f;
            dummySpreadGun0.multiplySpread = 1.0f;

            dummySpreadGun0.AddAttackAction(this.attackAction);
            // Action<GameObject> bigFlakSplit = delegate (GameObject bulletFired)
            // {
            //     ArcBulletModifier arc = bulletFired.GetOrAddComponent<ArcBulletModifier>();
            //     Miscs.Log("bigFlakSplit");
            // 
            //     arc.arcSpread = 1.0f;
            //     arc.bulletIndex = bulletFiredIndex;
            //     arc.bulletsInVolley = dummySpreadGun1.numberOfProjectiles;
            //     bulletFiredIndex++;
            // 
            //     Miscs.Log(bulletFiredIndex);
            // };
            // dummySpreadGun0.ShootPojectileAction = (Action<GameObject>)Delegate.Combine(gun.ShootPojectileAction, bigFlakSplit);

            //******//
            // fragmentation stats (Little Shrapnels: only damage, more projectiles)
            //******//

            Miscs.CopyGunStatsNoActions(playerOldGun, dummySpreadGun1);

            dummySpreadGun1.attackID = player.playerID;

            dummySpreadGun1.bursts = 0;
            dummySpreadGun1.timeBetweenBullets = 0.15f;

            dummySpreadGun1.numberOfProjectiles = Mathf.Clamp(flakProjectileAdd + Mathf.FloorToInt((float)playerOldGun.numberOfProjectiles / 10.0f), flakProjectileAdd, 3);

            dummySpreadGun1.damage = playerOldGun.damage * 0.50f;
            dummySpreadGun1.bulletDamageMultiplier = playerOldGun.bulletDamageMultiplier * 0.50f;

            dummySpreadGun1.projectileSpeed = Mathf.Clamp(playerOldGun.projectileSpeed * 0.75f, 0.5f, 25.0f);
            dummySpreadGun1.projectielSimulatonSpeed = Mathf.Clamp(playerOldGun.projectielSimulatonSpeed, 0.20f, 10.0f);

            dummySpreadGun1.evenSpread = 0.0f;
            dummySpreadGun1.spread = 0.5f;
            dummySpreadGun1.multiplySpread = 1.0f;

            dummySpreadGun1.objectsToSpawn = new ObjectsToSpawn[1];
            dummySpreadGun1.objectsToSpawn[0] = new ObjectsToSpawn()
            {
                AddToProjectile = FlakCannonCard.objectToSpawn
            };
            dummySpreadGun1.projectileColor = new Color(.40f, .10f, .10f, 1.0f);

            // dummySpreadGun.dragMinSpeed = 0.05f;

            List<ObjectsToSpawn> spawnList = playerOldGun.objectsToSpawn.ToList();
            ObjectsToSpawn spawn = new ObjectsToSpawn();
            spawn.AddToProjectile = bulletLifetimeFixer;
            spawnList.Add(spawn);
            dummySpreadGun0.objectsToSpawn = spawnList.ToArray();

            dummySpreadGun0.ShootPojectileAction = (projectile) =>
            {
                try
                {
                    ChompyBulletModifier chompyMono = projectile.transform.root.GetComponentInChildren<ChompyBulletModifier>();
                    if (chompyMono != null)
                    {
                        chompyMono.gameObject.AddComponent<ChompyBulletPetiteModifier>();
                        Destroy(chompyMono);
                    }

                    ArcBulletModifier arc = projectile.GetOrAddComponent<ArcBulletModifier>();
                    // Miscs.Log("bigFlakSplit");

                    arc.arcSpread = 1.0f;
                    arc.bulletIndex = bulletFiredIndex;
                    arc.bulletsInVolley = dummySpreadGun0.numberOfProjectiles + 1;
                    bulletFiredIndex++;

                    // Miscs.Log(bulletFiredIndex);
                }
                catch (Exception exception)
                {
                    Miscs.LogError("[GearUp] FlakShrapnel shoot action failed!");
                    Miscs.LogWarn(exception);
                }
            };

            //Miscs.Log("ApplySpreadFlak() : replacing gun");
            Miscs.CopyGunStats(newSpreadGun, gun);
            isGunReplaced = true;

            //Miscs.Log("ApplySpreadFlak() : combine delegate");
            this.gun.ShootPojectileAction = (Action<GameObject>)Delegate.Combine(this.gun.ShootPojectileAction, this.shootAction);
        }

        public void FixBurstAttack()
        {
            Gun targetGun;
            switch (stats.GetGearData().gunSpreadMod)
            {
                case GearUpConstants.ModType.gunSpreadArc:
                    targetGun = this.gun;
                    break;
                case GearUpConstants.ModType.gunSpreadLine:
                    return;
                case GearUpConstants.ModType.gunSpreadParallel:
                    targetGun = this.gun;
                    break;
                case GearUpConstants.ModType.gunSpreadFlak:
                    targetGun = this.gun;
                    break;
                default:
                    return;
            }

            int burstCount = Mathf.Clamp(targetGun.bursts, 1, 10);

            // clamp burst delay
            if (targetGun.timeBetweenBullets < 0.1f)
            {
                targetGun.timeBetweenBullets = 0.1f + (targetGun.timeBetweenBullets - 0.1f) * 0.2f;
            }
            else if (targetGun.timeBetweenBullets > 0.25f)
            {
                targetGun.timeBetweenBullets = 0.25f + (targetGun.timeBetweenBullets - 0.25f) * 0.2f;
            }

            targetGun.timeBetweenBullets = Mathf.Clamp(targetGun.timeBetweenBullets, 0.05f, 0.3f);

            // fix attack able to 'stack' bursts
            float burstTotalTime = targetGun.timeBetweenBullets * burstCount;
            if (burstTotalTime > targetGun.attackSpeed && burstCount > 1)
            {
                targetGun.attackSpeed = burstTotalTime;
            }
        }

        public float GetFlakCurrentDamageMultiplier()
        {
            return (gun.damage / newSpreadGun.damage) * (gun.bulletDamageMultiplier / newSpreadGun.bulletDamageMultiplier);
        }
    }

    class BulletNoClipModifier : MonoBehaviour
    {
        private const float tickTime = 0.1f;

        private float delayTime = 1.5f;
        private bool effectEnabled = false;
        private bool persistentOverride = false;

        internal bool previousState = true;
        internal float timer = 0.0f;

        public void Awake()
        {
            if (transform.parent != null)
            {
                previousState = transform.parent.Find("Collider").gameObject.activeSelf;
                transform.parent.Find("Collider").gameObject.SetActive(false);
                effectEnabled = true;
            }
        }

        public void Update()
        {
            if (effectEnabled)
            {
                timer += TimeHandler.deltaTime;

                if (!persistentOverride)
                {
                    if (timer >= delayTime)
                    {
                        RemoveModifier();
                    }
                }
                else
                {
                    // Persistent override the no bullet-bullet collision
                    if (timer >= tickTime)
                    {
                        if (transform.parent.Find("Collider").gameObject.activeSelf == false)
                        {
                            transform.parent.Find("Collider").gameObject.SetActive(false);
                        }

                        timer -= tickTime;
                    }
                }
            }
        }

        public void SetDuration(float duration)
        {
            delayTime = duration;
        }

        public void SetPersistentOverride(bool input)
        {
            persistentOverride = input;
        }

        public void RemoveModifier()
        {
            transform.parent.Find("Collider").gameObject.SetActive(previousState);
            Destroy(this);
        }

    }

    class ParallelBulletModifier : RayHitEffect
    {
        private static float parallelWidthScale = 20.0f;

        public int bulletIndex = 0;
        public int bulletsInVolley = 0;
        public float parallelWidth = 0.0f;
        private Vector3 sidewayVelocity = Vector3.zero;

        private float delayTime = 0.1f;
        private float initSpeed = 50.0f;

        internal float timer = 0.0f;

        private float prevGravity, prevSpeed, prevSimSpeed, prevDrag;
        private Vector3 targetDirection;

        private MoveTransform bulletMove;
        private ProjectileHit bulletHit;
        private Gun shooterGun;

        private bool effectEnable = false;

        public void Setup()
        {
            try
            {
                // Miscs.Log("[GearUp] ParallelBulletModifier: Update 0");
                bulletMove = gameObject.GetComponentInParent<MoveTransform>();

                // Miscs.Log("[GearUp] ParallelBulletModifier: Update 1");
                bulletHit = gameObject.GetComponentInParent<ProjectileHit>();

                // Miscs.Log("[GearUp] ParallelBulletModifier: Update 2");
                shooterGun = bulletHit.ownWeapon.GetComponent<Gun>();

                prevGravity = bulletMove.gravity;
                bulletMove.gravity = 0.0f;

                prevSpeed = bulletMove.velocity.magnitude;
                bulletMove.velocity = bulletMove.velocity.normalized * initSpeed;

                sidewayVelocity = Miscs.RotateVector(bulletMove.velocity.normalized, 90.0f);
                float sidewaySign = (bulletIndex % 2 == 0) ? -1.0f : 1.0f;
                float sidewayScale = (Mathf.Floor((bulletIndex + (bulletsInVolley % 2)) / 2) + (0.5f * ((bulletsInVolley + 1) % 2))) / (float)(bulletsInVolley - 1);
                sidewayVelocity = sidewayVelocity * sidewaySign * sidewayScale * parallelWidth * parallelWidthScale;

                // Miscs.Log($"[{bulletIndex}/{bulletsInVolley}] ({parallelWidth}): {sidewaySign} | {sidewayScale} | {sidewayVelocity}");

                bulletMove.velocity += sidewayVelocity;

                prevDrag = bulletMove.drag;
                bulletMove.drag = 0.0f;

                prevSimSpeed = Traverse.Create(bulletMove).Field("simulationSpeed").GetValue<float>();
                Traverse.Create(bulletMove).Field("simulationSpeed").SetValue((float)1.0f);

                targetDirection = shooterGun.shootPosition.forward;

                // Miscs.Log("[GearUp] ParallelBulletModifier: Start");
            }
            catch (Exception exception)
            {
                Miscs.LogWarn("[GearUP] ParallelBulletModifier: caught an exception");
                Miscs.LogWarn(exception);
                effectEnable = false;
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            // in case bullet hit anything before going parallel, immediately restore the stats and destroy this
            timer += delayTime;

            return HasToReturn.canContinue;
        }

        public void Update()
        {
            if (effectEnable)
            {
                timer += TimeHandler.deltaTime;

                if (timer >= delayTime)
                {
                    // plan B: delay alignment
                    bulletMove.gravity = prevGravity;
                    bulletMove.velocity = targetDirection * prevSpeed;
                    bulletMove.drag = prevDrag;
                    Traverse.Create(bulletMove).Field("simulationSpeed").SetValue((float) prevSimSpeed);

                    // Miscs.Log("[GearUp] ParallelBulletModifier: Updated and cleared");
                    Destroy(this);
                }
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
    }

    class ArcBulletModifier : RayHitEffect
    {
        public int bulletIndex = 0;
        public int bulletsInVolley = 0;
        public float arcSpread = 0.0f;

        private MoveTransform bulletMove;
        private ProjectileHit bulletHit;
        private Gun shooterGun;

        private bool effectEnable = false;

        // private float prevSpeed;

        public void Setup()
        {
            try
            {
                bulletMove = gameObject.GetComponentInParent<MoveTransform>();
                bulletHit = gameObject.GetComponentInParent<ProjectileHit>();
                shooterGun = bulletHit.ownWeapon.GetComponent<Gun>();

                // prevSpeed = bulletMove.velocity.magnitude;

                float sidewaySign = (bulletIndex % 2 == 0) ? -1.0f : 1.0f;
                float sidewayScale = (Mathf.Floor((bulletIndex + (bulletsInVolley % 2)) / 2) + (0.5f * ((bulletsInVolley + 1) % 2))) / (float)(bulletsInVolley - 1);
                float arcDegree = Mathf.Clamp(arcSpread, 0.0f, 1.0f) * 360.0f;

                bulletMove.velocity = Miscs.RotateVector(bulletMove.velocity, arcDegree * sidewayScale * sidewaySign);

                // Miscs.Log($"[{bulletIndex}/{bulletsInVolley}] ({arcSpread}): {sidewaySign} | {sidewayScale} | {arcDegree * sidewayScale * sidewaySign}");

                // Miscs.Log("[GearUp] ParallelBulletModifier: Start");
            }
            catch (Exception exception)
            {
                Miscs.LogWarn("[GearUP] ArcBulletModifier: caught an exception");
                Miscs.LogWarn(exception);
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            // in case bullet hit anything before going parallel, immediately restore the stats and destroy this
            return HasToReturn.canContinue;
        }

        public void Update()
        {
            if (!effectEnable)
            {
                MoveTransform moveTransform = GetComponentInParent<MoveTransform>();
                if (moveTransform != null)
                {
                    Setup();
                    effectEnable = true;
                }
            }
            // Destroy(this);
        }
    }

    class FlakShellModifier : RayHitEffect
    {
        private static GameObject vfxFlakShell = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_Part_FlakShell");
        private static Vector3 rotationDebug = new Vector3(270.0f, 270.0f, 0.0f);

        public static float defaultDelayTime = 0.85f;
        private float timer = 0.0f;

        private MoveTransform bulletMove;
        private ProjectileHit projectileHit;
        private Player shooterPlayer;
        private UniqueGunSpreadMono gunSpreadMono;
        private Gun dummyGunBig, dummyGunSmall;
        CharacterStatModifiers shooterStats;

        public bool effectEnable = false;
        public bool effectTriggered = false;
        public bool checkFlakShell = false;

        public bool isDirectlyHit = false;

        private GameObject partObj;
        private Vector3 playerPrevPos;
        private float delayTime = defaultDelayTime;
        private ProjectileHitEmpower empowerShotMono = null;

        float dmgMul = 1.0f;

        public void Start()
        {
            delayTime = defaultDelayTime;
        }

        public void Setup()
        {
            delayTime = defaultDelayTime;
            bulletMove = gameObject.GetComponentInParent<MoveTransform>();
            projectileHit = gameObject.GetComponentInParent<ProjectileHit>();
            shooterPlayer = projectileHit.ownPlayer;
            gunSpreadMono = shooterPlayer.gameObject.GetComponent<UniqueGunSpreadMono>();
            shooterStats = shooterPlayer.gameObject.GetComponent<CharacterStatModifiers>();
            dummyGunBig = gunSpreadMono.dummySpreadGun0;
            dummyGunSmall = gunSpreadMono.dummySpreadGun1;

            empowerShotMono = GetComponentInChildren<ProjectileHitEmpower>();
            if (empowerShotMono != null)
            {
                if (shooterStats.GetGearData().shieldBatteryStack == 0)
                {
                    dmgMul *= 1.5f;
                }
            }

            dmgMul *= gunSpreadMono.GetFlakCurrentDamageMultiplier();

            RemoveAfterSeconds removeMono = gameObject.GetComponentInChildren<RemoveAfterSeconds>();
            if (removeMono != null)
            {
                delayTime = Mathf.Clamp(removeMono.seconds * 2.0f, 0.0f, defaultDelayTime);
                removeMono.seconds *= 5.0f;
            }

            // visuals
            partObj = Instantiate(vfxFlakShell, transform.root);
            partObj.transform.localEulerAngles = rotationDebug;
            partObj.transform.localPosition = Vector3.zero;
        }

        public void Update()
        {
            if (!effectTriggered)
            {
                if (effectEnable)
                {
                    timer += TimeHandler.deltaTime;
                    if (timer >= delayTime)
                    {
                        // explode after timer
                        FlakExplode();
                    }
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

            if (partObj != null)
            {
                float damage = projectileHit.dealDamageMultiplierr * projectileHit.damage;
                float bulletSize = Mathf.Max(Mathf.Log(damage, 10.0f) * 0.5f, 0.5f);
                partObj.transform.localScale = Vector3.one * bulletSize;
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            if (effectTriggered) return HasToReturn.canContinue;

            // if (hit.transform != null)
            // {
            //     Miscs.Log("[GearUp] FlakShellModifier hit: " + hit.transform.gameObject.name);
            // }

            if (hit.transform.GetComponent<Player>())
            {
                // explode after bouncing off player
                if (!isDirectlyHit)
                {
                    isDirectlyHit = true;
                    timer = delayTime - 0.15f;
                }
            }
            if (hit.transform == null)
            {
                return HasToReturn.canContinue;
            }
            if (hit.transform.gameObject.tag.Contains("Bullet"))
            {
                return HasToReturn.canContinue;
            }

            return HasToReturn.canContinue;
        }

        public void FlakExplode()
        {
            // empty shell's dmg
            projectileHit.damage = 0.25f;
            
            if (dummyGunBig != null)
            {
                // playerPrevPos = shooterPlayer.transform.position;
                // shooterPlayer.transform.position = transform.position;

                // big shrapenls
                dummyGunBig.gameObject.transform.position = transform.position;

                Vector3 direction = bulletMove.velocity.normalized;
                Traverse.Create(dummyGunBig).Field("forceShootDir").SetValue((Vector3)direction);
                dummyGunBig.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);

                // small shrapnels
                this.ExecuteAfterFrames(1, () =>
                {
                    dummyGunSmall.gameObject.transform.position = transform.position;

                    Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(1.0f, 0.0f, 0.0f));
                    dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                    Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(-1.0f, 0.0f, 0.0f));
                    dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                });
                this.ExecuteAfterFrames(2, () =>
                {
                    dummyGunSmall.gameObject.transform.position = transform.position;

                    Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(0.0f, 1.0f, 0.0f));
                    dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                    Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(0.0f, -1.0f, 0.0f));
                    dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                });

                // this.ExecuteAfterFrames(2, () =>
                // {
                //     dummyGunSmall.gameObject.transform.position = transform.position;
                // 
                //     Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(0.5f, 0.866f, 0.0f));
                //     dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                //     Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(-0.5f, -0.866f, 0.0f));
                //     dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                // });
                // this.ExecuteAfterFrames(3, () =>
                // {
                //     dummyGunSmall.gameObject.transform.position = transform.position;
                // 
                //     Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(0.5f, -0.866f, 0.0f));
                //     dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                //     Traverse.Create(dummyGunSmall).Field("forceShootDir").SetValue((Vector3)new Vector3(-0.5f, 0.866f, 0.0f));
                //     dummyGunSmall.Attack(0.0f, true, useAmmo: false, damageM: dmgMul);
                // });

                // shooterPlayer.transform.position = playerPrevPos;
            }
            else
            {
                Miscs.LogWarn("[GearUp] FlakShellModifier: Dummy Gun is NULL!");
            }

            this.ExecuteAfterFrames(3, () =>
            {
                RayHitReflect rayHitReflect = GetComponentInChildren<RayHitReflect>();
                if (rayHitReflect != null)
                {
                    rayHitReflect.reflects = dummyGunBig.reflects;
                }

                if (empowerShotMono == null)
                {
                    // Miscs.Log("Boom!");
                    PhotonNetwork.Destroy(transform.root.gameObject);
                }
                else
                {
                    Destroy(partObj);
                    Destroy(this);
                }
            });

            effectTriggered = true;
        }
    }

    class BulletSpeedLimiter : MonoBehaviour
    {
        public const float defaultMinVelocity = 2.5f;
        public const float defaultMaxVelocity = 1000.0f;
        public const float defaultMinSimSpeed = 0.05f;
        public const float defaultMaxSimSpeed = 10.0f;

        private float minVelocity, maxVelocity, minSimSpeed, maxSimSpeed;
        private MoveTransform moveTransform = null;

        internal Vector3 directionNorm;
        internal float speed, simSpeed;

        public void Start()
        {
            Setup();
        }

        public void Update()
        {
            if (moveTransform == null)
            {
                moveTransform = GetComponentInParent<MoveTransform>();
            }
            else
            {
                speed = moveTransform.velocity.magnitude;
                directionNorm = moveTransform.velocity;
                directionNorm.z = 0.0f;
                directionNorm = directionNorm.normalized;

                speed = Mathf.Clamp(speed, minVelocity, maxVelocity);
                moveTransform.velocity = speed * directionNorm;

                // moveTransform.dragMinSpeed = minVelocity;

                simSpeed = Traverse.Create(moveTransform).Field("simulationSpeed").GetValue<float>();
                simSpeed = Mathf.Clamp(simSpeed, minSimSpeed, maxSimSpeed);
                Traverse.Create(moveTransform).Field("simulationSpeed").SetValue((float)simSpeed);
            }
        }

        public void Setup(float inMinVelo = defaultMinVelocity, float inMaxVelo = defaultMaxVelocity, float inMinSimSpeed = defaultMinSimSpeed, float inMaxSimSpeed = defaultMaxSimSpeed)
        {
            minVelocity = inMinVelo;
            maxVelocity = inMaxVelo;
            minSimSpeed = inMinSimSpeed;
            maxSimSpeed = inMaxSimSpeed;
        }
    }

    class BulletLifetimeFix : MonoBehaviour
    {
        MoveTransform moveTransform;

        public void Update()
        {
            moveTransform = GetComponentInParent<MoveTransform>();
            if (moveTransform != null)
            {
                ProjectileHit projectileHit = gameObject.GetComponentInParent<ProjectileHit>();
                Gun shooterGun = projectileHit.ownWeapon.GetComponent<Gun>();
                
                RemoveAfterSeconds removeMono = gameObject.GetComponentInParent<RemoveAfterSeconds>();
                if (shooterGun.destroyBulletAfter > 0.0f)
                {
                    if (removeMono == null)
                    {
                        removeMono = gameObject.GetOrAddComponent<RemoveAfterSeconds>();
                    }
                    removeMono.seconds = shooterGun.destroyBulletAfter;
                }

                Destroy(this);
            }
        }
    }

    // class NoFlakRecursion : MonoBehaviour
    // {
    //     public void Start()
    //     {
    //         ClearMono();
    //     }
    // 
    //     public void Awake()
    //     {
    //         ClearMono();
    //     }
    // 
    //     public void OnEnable()
    //     {
    //         ClearMono();
    //     }
    // 
    //     public void ClearMono()
    //     {
    //         FlakShellModifier[] mono = gameObject.GetComponentsInChildren<FlakShellModifier>();
    //         for (int i = mono.Length - 1; i >= 0; i--)
    //         {
    //             Destroy(mono[i]);
    //         }
    // 
    //         mono = gameObject.GetComponentsInParent<FlakShellModifier>();
    //         for (int i = mono.Length - 1; i >= 0; i--)
    //         {
    //             Destroy(mono[i]);
    //         }
    //     }
    // }
}
