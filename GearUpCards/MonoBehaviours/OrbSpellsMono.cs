﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

using UnityEngine;
using SoundImplementation;

using UnboundLib;
using UnboundLib.GameModes;

using HarmonyLib;
using Photon.Pun;
using ModdingUtils.MonoBehaviours;

using GearUpCards.Extensions;
using GearUpCards.Utils;
using UnboundLib.Utils;

namespace GearUpCards.MonoBehaviours
{
    public class OrbSpellsMono : MonoBehaviour
    {
        public static GameObject objectSpawn_obliteration = null;
        public static GameObject objectSpawn_rollingbulwark = null;
        public static GameObject objectSpawn_LFBlast = null;
        public static GameObject objectSpawn_LFDuality = null;

        private static string targetBounceCardName = "Target BOUNCE";
        private static GameObject screenEdgePrefab = null;
        // public float _debugScale = 2.0f;

        private static GameObject spellVFXOrbLiterate = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_VortexLoop");
        private static GameObject spellVFXRollingBuorbwark = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_EmpowerShot");

        private const string bulletGameObjectName = "Bullet_Base(Clone)";

        private const float procTime = .05f;
        private const float warmupTime = 0.5f;

        public enum OrbSpellType
        {
            none = 0,
            obliteration,
            lifeforceDuality,
            lifeforceBlast,
            rollingBulwark
        }

        public class OrbSpellStats
        {
            

            // on-block casts
            //   gun stats handled by outside methods
            //   orb stats handled by modifier mono

            // GameObject bulletModifier;
            GameObject orbDummyGunHolder;
            public OrbSpellType type;
            public Gun orbDummyGun;
            public int castPriority;

            public Action<GameObject> castAction = (_) => { };

            private float cooldownStats;
            private float cooldownTimer;

            private bool spellReady;
            private bool spellBursting;

            // Orb Sage stats, on-shoot cast
            // GameObject mainGunSpell;
            private int orbMaxCount;
            private int orbCount;

            private float burstTimer = 0.0f;

            public void SetupDummyGun(Gun playerGun, Player player)
            {
                orbDummyGunHolder = new GameObject("OrbSpell_");
                orbDummyGunHolder.transform.parent = player.transform;
                orbDummyGunHolder.transform.localPosition = Vector3.zero;

                orbDummyGun = orbDummyGunHolder.GetOrAddComponent<Gun>();
                Miscs.CopyGunStatsNoActions(playerGun, orbDummyGun);

                // orbDummyGun.name = "OrbCaster_";

                orbDummyGun.damage = 1.0f / 55.0f;

                orbDummyGun.dmgMOnBounce = 1.0f;
                orbDummyGun.bulletDamageMultiplier = 1.0f;
                orbDummyGun.damageAfterDistanceMultiplier = 1.0f;

                orbDummyGun.attackSpeed = 0.25f;
                orbDummyGun.attackSpeedMultiplier = 1.0f;
                orbDummyGun.defaultCooldown = 1.0f;

                orbDummyGun.spread = 0.0f;
                orbDummyGun.evenSpread = 0.0f;
                orbDummyGun.multiplySpread = 0.0f;

                orbDummyGun.numberOfProjectiles = 1;
                orbDummyGun.bursts = 0;
                orbDummyGun.timeBetweenBullets = 0.25f;

                orbDummyGun.reloadTime = 0.0f;
                orbDummyGun.reloadTimeAdd = 0.0f;

                orbDummyGun.recoil = 0.0f;
                orbDummyGun.recoilMuiltiplier = 0.0f;
                orbDummyGun.knockback = 0.01f;

                orbDummyGun.destroyBulletAfter = 0.0f;

                orbDummyGun.objectsToSpawn = new ObjectsToSpawn[] { };

                orbDummyGun.gravity = 0.0f;
                orbDummyGun.projectileSpeed = 1.0f;
                orbDummyGun.speedMOnBounce = 1.0f;
                orbDummyGun.projectielSimulatonSpeed = 1.0f;

                orbDummyGun.drag = 0f;
                orbDummyGun.dragMinSpeed = 1f;

                orbDummyGun.projectileColor = Color.black;

                // projectile size
                orbDummyGun.size = 0.0f;

                // bounces -- only 'reflects' works :P
                orbDummyGun.reflects = 0;
                orbDummyGun.randomBounces = 0;
                orbDummyGun.smartBounce = 0;

                orbDummyGun.unblockable = false;

                // GunAmmo gunAmmo = new GunAmmo();
                // gunAmmo.reloadTime = 0.0f;
                // gunAmmo.reloadTimeAdd = 0.0f;
                // gunAmmo.reloadTimeMultiplier = 0.0f;
                // gunAmmo.maxAmmo = 10;

                // Traverse.Create(gunAmmo).Field("reloadTimeMultiplier").SetValue((float) 0.0f);
                // Traverse.Create(gunAmmo).Field("reloadTimeAdd").SetValue((float) 0.0f);

                // Action doNothing = () => { };
                // Traverse.Create(orbDummyGun).Field("attackAction").SetValue((Action) doNothing);
                // orbDummyGun.ShootPojectileAction = new Action<GameObject>((GameObject _) => { });

                // disable unused/unrelated stats
                orbDummyGun.useCharge = false;
                orbDummyGun.currentCharge = 0.0f;

                orbDummyGun.chargeDamageMultiplier = 1.0f;
                orbDummyGun.chargeEvenSpreadTo = 0.0f;
                orbDummyGun.chargeNumberOfProjectilesTo = 0.0f;
                orbDummyGun.chargeRecoilTo = 0.0f;
                orbDummyGun.chargeSpeedTo = 1.0f;
                orbDummyGun.chargeSpreadTo = 0.0f;

                orbDummyGun.dontAllowAutoFire = true;

                orbDummyGun.explodeNearEnemyDamage = 0.0f;
                orbDummyGun.explodeNearEnemyRange = 0.0f;

                orbDummyGun.forceSpecificAttackSpeed = 0f;
                orbDummyGun.hitMovementMultiplier = 1.0f;

                orbDummyGun.ignoreWalls = false;
                orbDummyGun.isProjectileGun = false;
                orbDummyGun.isReloading = false;
                orbDummyGun.lockGunToDefault = false;

                orbDummyGun.overheatMultiplier = 0.0f;

                orbDummyGun.percentageDamage = 0.0f;

                orbDummyGun.shake = 0.0f;
                orbDummyGun.shakeM = 0.0f;

                orbDummyGun.sinceAttack = 0.0f;

                orbDummyGun.slow = 0.0f;

                orbDummyGun.spawnSkelletonSquare = false;
                orbDummyGun.teleport = false;

                orbDummyGun.timeToReachFullMovementMultiplier = 0f;

                orbDummyGun.cos = 0.0f;
                orbDummyGun.waveMovement = false;
            }

            public void SetupOrbSpell(OrbSpellType type, ObjectsToSpawn[] objectsToSpawns, float cooldownStats, int orbMaxCount, int castPriority)
            {
                this.type = type;

                orbDummyGun.objectsToSpawn = objectsToSpawns;

                this.cooldownStats = cooldownStats;
                this.orbMaxCount = orbMaxCount;
                this.castPriority = castPriority;
            }

            public void UpdateOrbSpell(float cooldownStats, int orbMaxCount)
            {
                this.cooldownStats = cooldownStats;
                this.orbMaxCount = orbMaxCount;
            }

            public OrbSpellStats(Gun playerGun, Player player)
            {
                SetupDummyGun(playerGun, player);
                WarmupOrbSpell();
            }

            public void WarmupOrbSpell()
            {
                spellReady = false;
                spellBursting = false;
                // cooldownTimer = cooldownStats - warmupTime;
                cooldownTimer = warmupTime;
            }

            public void RefreshOrbSpell()
            {
                spellReady = true;

                orbCount = orbMaxCount;
                spellBursting = false;

                cooldownTimer = 0.0f;
            }

            public void TickCooldown()
            {
                if (spellReady && orbMaxCount > 0)
                {
                    // just ignore the rest
                }
                else if (!spellReady && orbMaxCount > 0)
                {
                    // cooldownTimer += TimeHandler.deltaTime;
                    cooldownTimer -= TimeHandler.deltaTime;

                    if (spellBursting)
                    {
                        // if (cooldownTimer >= orbDummyGun.timeBetweenBullets)
                        // {
                        //     spellReady = true;
                        //     cooldownTimer = 0.0f;
                        // }

                        if (cooldownTimer <= 0.0f)
                        {
                            spellReady = true;
                            cooldownTimer = 0.0f;
                        }
                    }
                    else
                    {
                        // if (cooldownTimer >= cooldownStats)
                        // {
                        //     RefreshOrbSpell();
                        // }

                        if (cooldownTimer <= 0.0f)
                        {
                            RefreshOrbSpell();
                        }
                    }

                }
                else if (orbMaxCount <= 0)
                {
                    // if spell is disabled
                    spellReady = false;
                    cooldownTimer = 0.0f;
                }
            }

            public float GetCurrentCooldown()
            {
                if (!spellReady && !spellBursting)
                {
                    return cooldownTimer;
                }
                else if (orbMaxCount <= 0)
                {
                    return -11.0f;
                }
                else return -1.0f;
            }

            public int GetCurrentOrbCount()
            {
                if (orbCount > 0)
                {
                    return orbCount;
                }
                else if (orbMaxCount <= 0)
                {
                    return -11;
                }
                else
                {
                    return -1;
                }
            }

            public bool CheckSpellReady()
            {
                if (orbMaxCount > 0)
                {
                    return spellReady;
                }
                else 
                { 
                    return false;
                }
            }

            public bool CastOrbSpell()
            {
                if (spellReady)
                {
                    // fire method
                    orbDummyGun.Attack(0.0f, true, useAmmo: false);

                    // try
                    // {
                    //     GameObject bulletFired = ((SpawnedAttack)orbDummyGun.GetFieldValue("spawnedAttack")).gameObject;
                    //     bulletFired.name = "OrbSpell_Projectile";
                    // }
                    // catch (Exception exception)
                    // {
                    //     Miscs.LogWarn("[GearUp] OrbSpellsMono:OrbSpellStats bullet renaming failed!");
                    //     Miscs.LogWarn(exception);
                    // }

                    // also check if manual cast is enable
                    if (!spellBursting && orbCount > 1) 
                    {
                        // fire, trigger burst
                        spellBursting = true;
                        cooldownTimer = orbDummyGun.timeBetweenBullets;
                        orbCount--;
                    }
                    else if (spellBursting)
                    {
                        // fire, consume ammo

                        orbCount--;
                        if (orbCount <= 0)
                        {
                            spellBursting = false;
                            cooldownTimer = cooldownStats;
                        }
                        else
                        {
                            cooldownTimer = orbDummyGun.timeBetweenBullets;
                        }

                    }
                    else
                    {
                        // fire normally

                        orbCount--;
                        if (orbCount <= 0)
                        {
                            cooldownTimer = cooldownStats;
                            spellReady = false;
                        }
                        return true;
                    }

                    spellReady = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private List<OrbSpellStats> orbSpells;

        internal Action<BlockTrigger.BlockTriggerType> spellAction;

        internal Player player;
        internal GeneralInput generalInput;
        internal Gun gun;
        internal GunAmmo gunAmmo;
        internal Block block;
        internal CharacterStatModifiers stats;

        // ===== Spell modifiers =====
        // cooldown reduction and cast/burst speed
        internal int magickFragment = 0;
        // velocity and trajectory speed
        internal int glyphDivination = 0;
        // AoE and range
        internal int glyphInfluence = 0;
        // Bounces!
        internal int glyphGeometric = 0;
        // Spell power
        internal int glyphPotency = 0;
        // Spell duration
        internal int glyphTime = 0;
        // Spell projectiles
        internal int glyphReplication = 0;
        // Projectile piercing?
        // internal int glyphPiercing = 0;

        // Unique 'Gun' mod/class
        internal bool playerIsOrbSage = false;

        // internal Vector3 prevPosition;
        // internal Vector3 castPosition;

        // internal float timeLastBlocked = 0.0f;
        // internal float timeLastActivated = 0.0f;

        // ===== Player status handler =====
        private bool wasDeactivated = false;
        internal float timer = 0.0f;
        internal bool effectWarmUp = false;
        // internal int proc_count = 0;


        // ===== Orb Casting Handler =====
        // private bool spellAvailable = false;

        private int burstSpellIndex = 0;
        private float burstTimeStats = 0.0f;
        private float burstTimer = 0.0f;
        private bool burstCasting = false;
        private bool burstSwitching = false;

        public void Awake()
        {
            CardInfo cardInfo = CardManager.cards.Values.First(card => card.cardInfo.cardName == targetBounceCardName).cardInfo;
            Gun cardGun = cardInfo.gameObject.GetComponent<Gun>();
            screenEdgePrefab = cardGun.objectsToSpawn[1].AddToProjectile;

            this.gun = this.gameObject.GetComponent<WeaponHandler>().gun;
            this.gunAmmo = this.gun.GetComponentInChildren<GunAmmo>();
            this.player = this.gameObject.GetComponent<Player>();
            this.generalInput = this.gameObject.GetComponent<GeneralInput>();
            this.block = this.gameObject.GetComponent<Block>();
            this.stats = this.gameObject.GetComponent<CharacterStatModifiers>();

            GameModeManager.AddHook(GameModeHooks.HookRoundStart, OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Start()
        {
            // attach the new block effect and passing along reference to owner player's stats
            this.spellAction = new Action<BlockTrigger.BlockTriggerType>(this.GetDoBlockAction(this.player, this.block).Invoke);
            this.block.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Combine(this.block.BlockAction, this.spellAction);

            orbSpells = new List<OrbSpellStats>();
        }

        public void Update()
        {
            if (wasDeactivated)
            {
                // RESPAWNED: refresh all orb spell to "ready to Use"
                RefreshAllOrbSpells();

                wasDeactivated = false;
            }
            else
            {
                // update cooldown of orb spells normally
                TickOrbSpellCooldowns();
            }

            if (burstCasting)
            {
                TickBurstCastOrbSpell();
            }
        }

        private void TickOrbSpellCooldowns()
        {
            for (int i = 0; i < orbSpells.Count; i++)
            {
                orbSpells[i].TickCooldown();
            }
        }

        private void WarmupAllOrbSpells()
        {
            for (int i = 0; i < orbSpells.Count; i++)
            {
                orbSpells[i].WarmupOrbSpell();
            }
        }

        private void RefreshAllOrbSpells()
        {
            for (int i = 0; i < orbSpells.Count; i++)
            {
                orbSpells[i].RefreshOrbSpell();
            }
        }

        public int QueryOrbSpell(OrbSpellType type)
        {
            int result = -1;
            for (int i = 0; i < orbSpells.Count; i++)
            {
                if (orbSpells[i].type == type)
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        public float GetOrbSpellCooldown(int orbSpellIndex)
        {
            float cooldown = -1.0f;

            try
            {
                cooldown  = orbSpells[orbSpellIndex].GetCurrentCooldown();
            }
            catch (Exception exception)
            {
                Miscs.LogWarn(exception.ToString());
            }

            return cooldown;
        }

        private int InsertOrbSpell(OrbSpellStats newOrbSpell)
        {
            int result = orbSpells.Count;
            for (int i = 0; i < orbSpells.Count; i++)
            {
                if (orbSpells[i].castPriority > newOrbSpell.castPriority)
                {
                    result = i;
                    break;
                }
            }

            orbSpells.Insert(result, newOrbSpell);
            return result;
        }

        private bool CheckOrbSpellAvailable()
        {
            // if (burstCasting)
            // {
            //     return false;
            // }

            for (int i = 0; i < orbSpells.Count; i++)
            {
                if (orbSpells[i].CheckSpellReady())
                {
                    burstSpellIndex = i;
                    return true;
                }
            }

            burstSpellIndex = -1;
            return false;
        }

        private bool CheckNextOrbSpellAvailable()
        {
            // if (!burstCasting)
            // {
            //     return false;
            // }

            for (int i = burstSpellIndex; i < orbSpells.Count; i++)
            {
                if (orbSpells[i].CheckSpellReady())
                {
                    burstSpellIndex = i;
                    return true;
                }
            }

            burstSpellIndex = -1;
            return false;
        }

        private void TickBurstCastOrbSpell()
        {
            if (burstSwitching)
            {
                burstTimer += TimeHandler.deltaTime;

                if (burstTimer >= burstTimeStats)
                {
                    burstTimer = 0.0f;
                    burstSwitching = false;

                    if (!CheckOrbSpellAvailable())
                    {
                        burstCasting = false;
                    }
                }
            }
            else if (orbSpells[burstSpellIndex].CheckSpellReady())
            {
                // Redirect Orb Spell
                SetGunForceDir(orbSpells[burstSpellIndex].orbDummyGun);

                orbSpells[burstSpellIndex].CastOrbSpell();

                if (orbSpells[burstSpellIndex].GetCurrentOrbCount() <= 0)
                {
                    burstSwitching = true;
                }
            }
        }

        private void SetGunForceDir(Gun gun)
        {
            Vector3 shootDir;

            // determine direction and portal pos
            if (generalInput.inputType == GeneralInput.InputType.Controller)
            {
                shootDir = generalInput.lastAimDirection;
            }
            else
            {
                shootDir = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition) - player.transform.position;
            }

            shootDir.z = 0.0f;
            shootDir = shootDir.normalized;

            gun.SetFieldValue("forceShootDir", (Vector3)shootDir);
        }

        private void SetGunForceDir(Gun gun, Vector3 shootDir)
        {
            gun.SetFieldValue("forceShootDir", (Vector3)shootDir);
        }

        private void UpdateOrbSpellStats()
        {
            // General Stats
            magickFragment = stats.GetGearData().glyphMagickFragment;
            glyphDivination = stats.GetGearData().glyphDivination;
            glyphGeometric = stats.GetGearData().glyphGeometric;
            glyphInfluence = stats.GetGearData().glyphInfluence;
            glyphPotency = stats.GetGearData().glyphPotency;
            glyphTime = stats.GetGearData().glyphTime;
            glyphReplication = stats.GetGearData().glyphReplication;

            // burstTimeStats = Mathf.Clamp(0.3f - (magickFragment * 0.05f), 0.1f, 1.0f);

            int checkIndex;

            // Orb Spell Obliteration
            checkIndex = QueryOrbSpell(OrbSpellType.obliteration);
            if (stats.GetGearData().orbObliterationStack > 0)
            {
                // stats calculation
                float cooldown = Mathf.Clamp(8.0f - (magickFragment * 1.0f), 3.0f, 15.0f);
                float burstTime = Mathf.Clamp(0.3f - (magickFragment * 0.05f), 0.1f, 0.5f);
                int orbCount = Mathf.FloorToInt((2.1f + stats.GetGearData().orbObliterationStack) / 2.0f);
                int bounceCount = glyphGeometric;  //Mathf.FloorToInt((3.1f + glyphGeometric) / 2.0f);
                float orbVelocity = 0.5f + (glyphDivination * 0.25f);
                float orbSpeed = 1.0f + (glyphDivination * 0.1f);

                // Action<GameObject> tempAction;

                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    // if in list: Update and enable
                    orbSpells[checkIndex].UpdateOrbSpell(cooldown, orbCount);
                }
                else
                {
                    // else: create and insert by priority
                    OrbSpellStats newOrbSpell = new OrbSpellStats(gun, player);

                    newOrbSpell.castAction = (orbCast) =>
                    {
                        try
                        {
                            orbCast.name = "OrbSpell_Obliteration";
                            GameObject addToOrb = new GameObject("OrbSpell_ObliterationModifier");
                            addToOrb.transform.parent = orbCast.transform;
                            addToOrb.transform.localPosition = Vector3.zero;

                            addToOrb.AddComponent<OrbObliterationModifier>();
                            addToOrb.AddComponent<SyncBulletPosition>();

                            BulletNoClipModifier noClipper = addToOrb.AddComponent<BulletNoClipModifier>();
                            noClipper.SetPersistentOverride(true);
                        }
                        catch (Exception exception)
                        {
                            Miscs.LogError("[GearUp] OrbSpell_Obliteration cast action failed!");
                            Miscs.LogWarn(exception);
                        }
                    };

                    Action<GameObject> tempAction = new Action<GameObject>(ShootActionAddVFX(spellVFXOrbLiterate));
                    newOrbSpell.orbDummyGun.ShootPojectileAction = (Action<GameObject>)Delegate.Combine
                    (
                        newOrbSpell.castAction,
                        tempAction
                    );

                    ObjectsToSpawn[] spawnObjects = new ObjectsToSpawn[]
                    {
                        new ObjectsToSpawn
                        {
                            AddToProjectile = screenEdgePrefab
                        }
                    };

                    newOrbSpell.SetupOrbSpell(OrbSpellType.obliteration, spawnObjects, cooldown, orbCount, 10);
                    newOrbSpell.orbDummyGun.name = "OrbSpellHolder_Obliteration";

                    checkIndex = InsertOrbSpell(newOrbSpell);
                }

                orbSpells[checkIndex].orbDummyGun.projectileColor = new Color(0.4f, 0.0f, 0.4f, 1.0f);
                orbSpells[checkIndex].orbDummyGun.gravity = 0.0f;
                orbSpells[checkIndex].orbDummyGun.projectileSpeed = orbVelocity;
                orbSpells[checkIndex].orbDummyGun.projectielSimulatonSpeed = orbSpeed;
                orbSpells[checkIndex].orbDummyGun.timeBetweenBullets = burstTime;
                orbSpells[checkIndex].orbDummyGun.reflects = bounceCount;

                //replcation!
                if (glyphReplication > 0)
                {
                    orbSpells[checkIndex].orbDummyGun.evenSpread = 1.0f;
                    orbSpells[checkIndex].orbDummyGun.spread = 0.01f;

                    int projectiles = 1 + glyphReplication;
                    orbSpells[checkIndex].orbDummyGun.numberOfProjectiles = projectiles;

                    if (projectiles <= 4)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 5.0f;
                    }
                    else if (projectiles <= 10)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 6.0f;
                    }
                    else
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 7.0f;
                    }
                }

                Miscs.Log("[GearUp] OrbSpellsMono: Obliteration Updated!");
            }
            else
            {
                // check if in list

                // if in list: Update to zero and disable
                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    orbSpells[checkIndex].UpdateOrbSpell(10.0f, 0);
                }
            }

            // Orb Spell Rolling-Bulwark
            checkIndex = QueryOrbSpell(OrbSpellType.rollingBulwark);
            if (stats.GetGearData().orbRollingBulwarkStack > 0)
            {
                // stats calculation
                float cooldown = Mathf.Clamp(6.0f - (magickFragment * 0.5f), 3.0f, 10.0f);
                float burstTime = Mathf.Clamp(0.35f - (magickFragment * 0.05f), 0.1f, 0.5f);
                int orbCount = 1 + Mathf.FloorToInt((stats.GetGearData().orbRollingBulwarkStack) / 2.0f);
                int bounceCount = 2 + glyphGeometric;
                float orbVelocity = 0.75f + (glyphDivination * 0.25f);
                float orbSpeed = 1.0f + (glyphDivination * 0.1f);
                // int empowerStack = 1 + Mathf.FloorToInt((1.1f + (float)glyphPotency) / 2.0f);

                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    // if in list: Update and enable
                    orbSpells[checkIndex].UpdateOrbSpell(cooldown, orbCount);
                }
                else
                {
                    // else: create and insert by priority
                    OrbSpellStats newOrbSpell = new OrbSpellStats(gun, player);

                    newOrbSpell.castAction = (orbCast) =>
                    {
                        try
                        {
                            orbCast.name = "OrbSpell_RollingBulwark";
                            GameObject addToOrb = new GameObject("RollingBulwarkModifier");
                            addToOrb.transform.parent = orbCast.transform;
                            addToOrb.transform.localPosition = Vector3.zero;

                            addToOrb.AddComponent<SyncBulletPosition>();

                            CustomEmpowerShotModifier customEmpower = addToOrb.AddComponent<CustomEmpowerShotModifier>();
                            customEmpower.SetupEmpowerCharge(
                                3 + glyphGeometric,
                                stats.GetGearData().orbRollingBulwarkStack + Mathf.FloorToInt((1.1f + (float)glyphPotency) / 2.0f)
                            );

                            BulletNoClipModifier noClipper = addToOrb.AddComponent<BulletNoClipModifier>();
                            noClipper.SetPersistentOverride(true);
                        }
                        catch (Exception exception)
                        {
                            Miscs.LogError("[GearUp] OrbSpell_RollingBulwark cast action failed!");
                            Miscs.LogWarn(exception);
                        }
                    };

                    Action<GameObject> tempAction2 = new Action<GameObject>(ShootActionAddVFX(spellVFXRollingBuorbwark));
                    newOrbSpell.orbDummyGun.ShootPojectileAction = (Action<GameObject>)Delegate.Combine
                    (
                        newOrbSpell.castAction,
                        tempAction2
                    );

                    ObjectsToSpawn[] spawnObjects = new ObjectsToSpawn[]
                    {
                        new ObjectsToSpawn
                        {
                            AddToProjectile = screenEdgePrefab
                        }
                    };

                    newOrbSpell.SetupOrbSpell(OrbSpellType.rollingBulwark, spawnObjects, cooldown, orbCount, 20);
                    newOrbSpell.orbDummyGun.name = "OrbSpellHolder_RollingBulwark";

                    checkIndex = InsertOrbSpell(newOrbSpell);
                }

                orbSpells[checkIndex].orbDummyGun.projectileColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
                orbSpells[checkIndex].orbDummyGun.gravity = 0.0f;
                orbSpells[checkIndex].orbDummyGun.projectileSpeed = orbVelocity;
                orbSpells[checkIndex].orbDummyGun.projectielSimulatonSpeed = orbSpeed;
                orbSpells[checkIndex].orbDummyGun.timeBetweenBullets = burstTime;
                orbSpells[checkIndex].orbDummyGun.reflects = bounceCount;

                //replcation!
                if (glyphReplication > 0)
                {
                    orbSpells[checkIndex].orbDummyGun.evenSpread = 1.0f;
                    orbSpells[checkIndex].orbDummyGun.spread = 0.01f;

                    int projectiles = 1 + glyphReplication;
                    orbSpells[checkIndex].orbDummyGun.numberOfProjectiles = projectiles;

                    if (projectiles <= 4)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 5.0f;
                    }
                    else if (projectiles <= 10)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 6.0f;
                    }
                    else
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 7.0f;
                    }
                }

                Miscs.Log("[GearUp] OrbSpellsMono: Rolling-Bulwark Updated!");
            }
            else
            {
                // check if in list

                // if in list: Update to zero and disable
                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    orbSpells[checkIndex].UpdateOrbSpell(10.0f, 0);
                }
            }

            // Orb Spell Lifeforce Duality
            checkIndex = QueryOrbSpell(OrbSpellType.lifeforceDuality);
            if (stats.GetGearData().orbLifeforceDualityStack > 0)
            {
                // stats calculation
                float cooldown = Mathf.Clamp(8.0f - (magickFragment * 0.5f), 4.0f, 10.0f);
                float burstTime = Mathf.Clamp(0.30f - (magickFragment * 0.05f), 0.1f, 0.5f);
                int orbCount = stats.GetGearData().orbLifeforceDualityStack;
                int bounceCount = 500 + (glyphGeometric * 100);
                float orbVelocity = 0.60f + (glyphDivination * 0.10f);
                float orbSpeed = 1.0f + (glyphDivination * 0.1f);

                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    // if in list: Update and enable
                    orbSpells[checkIndex].UpdateOrbSpell(cooldown, orbCount);
                }
                else
                {
                    // else: create and insert by priority
                    OrbSpellStats newOrbSpell = new OrbSpellStats(gun, player);

                    newOrbSpell.castAction = (orbCast) =>
                    {
                        try
                        {
                            orbCast.name = "OrbSpell_LifeforceDuality";
                            GameObject addToOrb = new GameObject("LifeforceDualityModifier");
                            addToOrb.transform.parent = orbCast.transform;
                            addToOrb.transform.localPosition = Vector3.zero;

                            addToOrb.AddComponent<OrbLifeforceDualityModifier>();
                            addToOrb.AddComponent<SyncBulletPosition>();

                            BulletNoClipModifier noClipper = addToOrb.AddComponent<BulletNoClipModifier>();
                            noClipper.SetPersistentOverride(true);
                        }
                        catch (Exception exception)
                        {
                            Miscs.LogError("[GearUp] OrbSpell_LifeforceDuality cast action failed!");
                            Miscs.LogWarn(exception);
                        }
                    };

                    newOrbSpell.orbDummyGun.ShootPojectileAction = newOrbSpell.castAction;

                    ObjectsToSpawn[] spawnObjects = new ObjectsToSpawn[]
                    {
                        new ObjectsToSpawn
                        {
                            AddToProjectile = screenEdgePrefab
                        }
                    };

                    newOrbSpell.SetupOrbSpell(OrbSpellType.lifeforceDuality, spawnObjects, cooldown, orbCount, 5);
                    newOrbSpell.orbDummyGun.name = "OrbSpellHolder_LifeforceDuality";

                    checkIndex = InsertOrbSpell(newOrbSpell);
                }

                orbSpells[checkIndex].orbDummyGun.projectileColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
                orbSpells[checkIndex].orbDummyGun.gravity = 0.0f;
                orbSpells[checkIndex].orbDummyGun.projectileSpeed = orbVelocity;
                orbSpells[checkIndex].orbDummyGun.projectielSimulatonSpeed = orbSpeed;
                orbSpells[checkIndex].orbDummyGun.timeBetweenBullets = burstTime;
                orbSpells[checkIndex].orbDummyGun.reflects = bounceCount;

                //replcation!
                if (glyphReplication > 0)
                {
                    orbSpells[checkIndex].orbDummyGun.evenSpread = 1.0f;
                    orbSpells[checkIndex].orbDummyGun.spread = 0.01f;

                    int projectiles = 1 + glyphReplication;
                    orbSpells[checkIndex].orbDummyGun.numberOfProjectiles = projectiles;

                    if (projectiles <= 4)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 5.0f;
                    }
                    else if (projectiles <= 10)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 6.0f;
                    }
                    else
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 7.0f;
                    }
                }

                Miscs.Log("[GearUp] OrbSpellsMono: Lifeforce Duality Updated!");
            }
            else
            {
                // check if in list

                // if in list: Update to zero and disable
                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    orbSpells[checkIndex].UpdateOrbSpell(10.0f, 0);
                }
            }

            // Orb Spell Lifeforce Blast
            checkIndex = QueryOrbSpell(OrbSpellType.lifeforceBlast);
            if (stats.GetGearData().orbLifeforceBlastStack > 0)
            {
                // stats calculation
                float cooldown = Mathf.Clamp(8.0f - (magickFragment * 0.5f), 4.0f, 10.0f);
                float burstTime = Mathf.Clamp(0.30f - (magickFragment * 0.05f), 0.1f, 0.5f);
                int orbCount = Mathf.FloorToInt((2.1f + stats.GetGearData().orbLifeforceBlastStack) / 2.0f);
                int bounceCount = glyphGeometric;
                float orbVelocity = 0.75f + (glyphDivination * 0.15f);
                float orbSpeed = 1.0f + (glyphDivination * 0.1f);

                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    // if in list: Update and enable
                    orbSpells[checkIndex].UpdateOrbSpell(cooldown, orbCount);
                }
                else
                {
                    // else: create and insert by priority
                    OrbSpellStats newOrbSpell = new OrbSpellStats(gun, player);

                    newOrbSpell.castAction = (orbCast) =>
                    {
                        try
                        {
                            orbCast.name = "OrbSpell_LifeforceBlast";
                            GameObject addToOrb = new GameObject("LifeforceBlastModifier");
                            addToOrb.transform.parent = orbCast.transform;
                            addToOrb.transform.localPosition = Vector3.zero;

                            addToOrb.AddComponent<OrbLifeforceBlastModifier>();
                            addToOrb.AddComponent<SyncBulletPosition>();

                            BulletNoClipModifier noClipper = addToOrb.AddComponent<BulletNoClipModifier>();
                            noClipper.SetPersistentOverride(true);
                        }
                        catch (Exception exception)
                        {
                            Miscs.LogError("[GearUp] OrbSpell_LifeforceBlast cast action failed!");
                            Miscs.LogWarn(exception);
                        }
                    };

                    newOrbSpell.orbDummyGun.ShootPojectileAction = newOrbSpell.castAction;

                    ObjectsToSpawn[] spawnObjects = new ObjectsToSpawn[]
                    {
                        new ObjectsToSpawn
                        {
                            AddToProjectile = screenEdgePrefab
                        }
                    };

                    newOrbSpell.SetupOrbSpell(OrbSpellType.lifeforceBlast, spawnObjects, cooldown, orbCount, 5);
                    newOrbSpell.orbDummyGun.name = "OrbSpellHolder_LifeforceBlast";

                    checkIndex = InsertOrbSpell(newOrbSpell);
                }

                orbSpells[checkIndex].orbDummyGun.projectileColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
                orbSpells[checkIndex].orbDummyGun.gravity = 0.0f;
                orbSpells[checkIndex].orbDummyGun.projectileSpeed = orbVelocity;
                orbSpells[checkIndex].orbDummyGun.projectielSimulatonSpeed = orbSpeed;
                orbSpells[checkIndex].orbDummyGun.timeBetweenBullets = burstTime;
                orbSpells[checkIndex].orbDummyGun.reflects = bounceCount;

                //replcation!
                if (glyphReplication > 0)
                {
                    orbSpells[checkIndex].orbDummyGun.evenSpread = 1.0f;
                    orbSpells[checkIndex].orbDummyGun.spread = 0.01f;

                    int projectiles = 1 + glyphReplication;
                    orbSpells[checkIndex].orbDummyGun.numberOfProjectiles = projectiles;

                    if (projectiles <= 4)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 5.0f;
                    }
                    else if (projectiles <= 10)
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 6.0f;
                    }
                    else
                    {
                        orbSpells[checkIndex].orbDummyGun.multiplySpread = projectiles * 7.0f;
                    }
                }

                Miscs.Log("[GearUp] OrbSpellsMono: Lifeforce Blast Updated!");
            }
            else
            {
                // check if in list

                // if in list: Update to zero and disable
                if (checkIndex >= 0 && checkIndex < orbSpells.Count)
                {
                    orbSpells[checkIndex].UpdateOrbSpell(10.0f, 0);
                }
            }

            // repeat for each spell manually
        }

        // a work around the delegate limits, cheeky!
        public Action<BlockTrigger.BlockTriggerType> GetDoBlockAction(Player player, Block block)
        {
            return delegate (BlockTrigger.BlockTriggerType trigger)
            {
                bool conditionMet = (trigger == BlockTrigger.BlockTriggerType.Default && !burstCasting);

                if (conditionMet)
                {
                    // If not Orb Sage > trigger Orb Spell burst-casting
                    if (!playerIsOrbSage)
                    {
                        if (CheckOrbSpellAvailable())
                        {
                            burstCasting = true;
                            // TickBurstCastOrbSpell();
                        }
                    }
                }
            };
        }

        private IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            // refresh Orb Spells list
            UpdateOrbSpellStats();

            yield break;
        }

        private IEnumerator OnPointStart(IGameModeHandler gm)
        {
            // Set all Orb Spells cooldown
            WarmupAllOrbSpells();

            effectWarmUp = true;
            wasDeactivated = false;

            yield break;
        }

        private IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            effectWarmUp = false;

            yield break;
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            // stop and reset burst casting
            // spellAvailable = false;
            burstTimer = 0.0f;
            burstCasting = false;
            burstSwitching = false;
            burstSpellIndex = 0;

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
                // resolve death and respawning
                wasDeactivated = true;

                // stop and reset burst casting
                // spellAvailable = false;
                burstTimer = 0.0f;
                burstCasting = false;
                burstSwitching = false;
                burstSpellIndex = 0;

                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - dead ded!?");
            }

        }

        public void OnDestroy()
        {
            // This effect should persist between rounds, and at 0 stack it should do nothing mechanically
            // UnityEngine.Debug.Log($"Destroying Scanner  [{this.player.playerID}]");

            GameModeManager.RemoveHook(GameModeHooks.HookRoundStart, OnRoundStart);
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.RemoveHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);

            this.block.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Remove(this.block.BlockAction, this.spellAction);
            // UnityEngine.Debug.Log($"Scanner destroyed  [{this.player.playerID}]");
        }

        // Action Delegates
        public Action<GameObject> ShootActionAddVFX(GameObject VFXPrefab)
        {
            return delegate (GameObject bulletFired)
            {
                try
                {
                    this.ExecuteAfterFrames(1, () =>
                    {
                        GameObject VFX = Instantiate(VFXPrefab, bulletFired.transform);
                        VFX.transform.up = bulletFired.transform.forward;
                        // arrow.transform.localScale /= 2;
                    });
                }
                catch (Exception)
                {
                    UnityEngine.Debug.LogWarning($"[OrbSpellMono] ShootActionAddVFX failed! [{player.playerID}]");
                }
                
            };
        }
    }

}
