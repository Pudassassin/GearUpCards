using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

using UnityEngine;
using SoundImplementation;

using UnboundLib;
using UnboundLib.GameModes;
using Photon.Pun;
using ModdingUtils.MonoBehaviours;

using GearUpCards.Extensions;
using HarmonyLib;

namespace GearUpCards.MonoBehaviours
{
    internal class AntiBulletMagickEffect : MonoBehaviour
    {
        // public float _debugScale = 2.0f;

        private static GameObject spellVFXPrefab = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_AntiBulletMagick");

        private const string bulletGameObjectName = "Bullet_Base(Clone)";
        private const float spellCooldownBase = 12.0f;
        private const float spellForceReloadTimeAddBase = 3.5f;
        private const float spellRangeBase = 12.5f;
        private const float spellDurationBase = 2.0f;
        private const float spellForceReloadSelfMulBase = 0.5f;

        public float spellCastDelay = 0.1f;
        public float spellCastDelayTimer = 0.0f;
        public bool spellIsCast = false;
        public BlockTrigger.BlockTriggerType spellTrigger;

        private const float procTime = 0.01f;
        private const float warmupTime = 0.5f;

        internal Action<BlockTrigger.BlockTriggerType> spellAction;

        // ===== Spell modifiers =====
        // cooldown reduction and reduce self-effect
        internal int magickFragment = 0;
        // AoE and range
        internal int glyphInfluence = 0;
        // Force reload time increases
        internal int glyphPotency = 0;
        // Zone duration
        internal int glyphTime = 0;
        // Self-debuff resistance
        internal int glyphProtection = 0;

        internal float spellCooldown = 12.0f;
        internal float spellRange = 10.0f;
        internal float spellForceReloadTimeAdd = 3.5f;
        internal float spellDuration = 1.5f;
        internal float spellForceReloadSelfMul = 0.5f;

        internal bool spellReady = false;
        internal bool empowerCharged = false;

        internal Vector3 prevPosition;

        internal Vector3 castPosition;
        internal Vector3 castPosEmpower;

        // internal float timeLastBlocked = 0.0f;
        // internal float timeLastActivated = 0.0f;

        public float CooldownTimer = 0.0f;

        public float DurationTimer = 0.0f;
        public float DurationTimerE = 0.0f;

        internal float timer = 0.0f;
        internal bool effectWarmUp = false;
        // internal int proc_count = 0;

        internal Player player;
        internal Block block;
        internal CharacterStatModifiers stats;


        public void Awake()
        {
            this.player = this.gameObject.GetComponent<Player>();
            this.block = this.gameObject.GetComponent<Block>();
            this.stats = this.gameObject.GetComponent<CharacterStatModifiers>();

            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Start()
        {
            // attach the new block effect and passing along reference to owner player's stats
            this.spellAction = new Action<BlockTrigger.BlockTriggerType>(this.GetDoBlockAction(this.player, this.block).Invoke);
            this.block.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Combine(this.block.BlockAction, this.spellAction);
        }

        public void Update()
        {
            timer += TimeHandler.deltaTime;
            if (CooldownTimer > 0.0f)
            { 
                CooldownTimer -= TimeHandler.deltaTime;
            }

            if (DurationTimer > 0.0f)
            {
                DurationTimer -= TimeHandler.deltaTime;
            }
            if (DurationTimerE > 0.0f)
            {
                DurationTimerE -= TimeHandler.deltaTime;
            }

            if (spellIsCast)
            {
                if (spellCastDelayTimer > 0.0f)
                {
                    spellCastDelayTimer -= TimeHandler.deltaTime;
                }
                else
                {
                    spellCastDelayTimer = 0.0f;
                    spellIsCast = false;

                    CastSpell(player.transform.position, spellTrigger);
                }
            }

            if (timer >= procTime)
            {
                prevPosition = this.player.transform.position;
                if (this.stats.GetGearData().uniqueMagick == GearUpConstants.ModType.magickAntiBullet)
                {
                    RecalculateEffectStats();
                    CheckReady();
                }
                else
                {
                    empowerCharged = false;
                    spellReady = false;
                }

                //if (Time.time < timeLastActivated + spellDuration)
                if (DurationTimer > 0.0f)
                {
                    DeleteBullet();
                }
                if (DurationTimerE > 0.0f)
                {
                    DeleteBulletE();
                }

                timer -= procTime;
                // proc_count++;
            }

            this.stats.GetGearData().t_uniqueMagickCooldown = GetCooldown();
        }

        internal void CheckReady()
        {
            if (spellReady)
            {
                return;
            }
            // else if (Time.time >= timeLastBlocked + spellCooldown)
            else if (CooldownTimer <= 0.0f)
            {
                spellReady = true;
                CooldownTimer = 0.0f;
            }
        }

        public float GetCooldown()
        {
            // float cooldown = timeLastBlocked + spellCooldown - Time.time;
            if (spellReady) return -1.0f;
            else return CooldownTimer;
        }

        // a work around the delegate limits, cheeky!
        public Action<BlockTrigger.BlockTriggerType> GetDoBlockAction(Player player, Block block)
        {
            return delegate (BlockTrigger.BlockTriggerType trigger)
            {
                bool conditionMet = (trigger == BlockTrigger.BlockTriggerType.Empower && empowerCharged) ||
                                    (trigger == BlockTrigger.BlockTriggerType.Default && spellReady) ||
                                    (trigger == BlockTrigger.BlockTriggerType.ShieldCharge && spellReady);

                if (conditionMet)
                {
                    // empower do cheeky teleport, I can just grab player.transform.position

                    if (trigger == BlockTrigger.BlockTriggerType.Empower)
                    {
                        castPosEmpower = this.player.transform.position;
                        DurationTimerE = spellDuration;

                        empowerCharged = false;

                        CastSpell(castPosEmpower, trigger);
                        // timeLastActivated = Time.time;
                    }
                    else
                    {
                        castPosition = this.player.transform.position;
                        DurationTimer = spellDuration;

                        switch (trigger)
                        {
                            case BlockTrigger.BlockTriggerType.Default:
                                spellCastDelayTimer = spellCastDelay;
                                break;
                            case BlockTrigger.BlockTriggerType.ShieldCharge:
                                spellCastDelayTimer = spellCastDelay + 0.5f;
                                break;
                            default:
                                spellCastDelayTimer = 0.0f;
                                break;
                        }
                        spellIsCast = true;
                        spellTrigger = trigger;

                        // timeLastBlocked = Time.time;
                        // timeLastActivated = Time.time;
                        CooldownTimer = spellCooldown;
                        spellReady = false;
                        empowerCharged = true;
                    }
                }
            };
        }

        public void CastSpell(Vector3 position, BlockTrigger.BlockTriggerType trigger)
        {
            // VFX Part
            GameObject VFX = Instantiate(spellVFXPrefab, this.player.transform.position + new Vector3(0.0f, 0.0f, 100.0f), Quaternion.identity);
            VFX.transform.localScale = Vector3.one * spellRange;
            VFX.name = "AntiBulletVFX_Copy";
            VFX.GetComponent<Canvas>().sortingLayerName = "MostFront";
            VFX.GetComponent<Canvas>().sortingOrder = 10000;

            VFX.AddComponent<RemoveAfterSeconds>().seconds = spellDuration;

            // check players in range and apply status
            float distance;

            DeleteBullet();

            foreach (Player target in PlayerManager.instance.players)
            {
                if (target.playerID == this.player.playerID && trigger == BlockTrigger.BlockTriggerType.Empower)
                {
                    // resolve empower position reversing its trickery
                    distance = (this.prevPosition - position).magnitude;
                }
                else
                {
                    distance = (target.transform.position - position).magnitude;
                }

                if (distance > spellRange)
                    continue;

                // UnityEngine.Debug.Log($"[AntiBullet] Reading player[{target.playerID}]");

                Gun targetGun = target.gameObject.GetComponent<WeaponHandler>().gun;
                GunAmmo targetGunAmmo = target.gameObject.GetComponent<WeaponHandler>().gun.GetComponentInChildren<GunAmmo>();

                // UnityEngine.Debug.Log($"[AntiBullet] Applying ForceReload to player[{target.playerID}]");
                if (target.playerID == this.player.playerID)
                {
                    ApplyForceReload(targetGun, targetGunAmmo, spellForceReloadTimeAdd * spellForceReloadSelfMul);
                }
                else
                {
                    ApplyForceReload(targetGun, targetGunAmmo, spellForceReloadTimeAdd);
                }

                // UnityEngine.Debug.Log($"[AntiBullet] Forced-Reload player[{target.playerID}]");

            }
        }

        internal void RecalculateEffectStats()
        {
            magickFragment = this.stats.GetGearData().glyphMagickFragment;
            glyphInfluence = this.stats.GetGearData().glyphInfluence;
            glyphPotency = this.stats.GetGearData().glyphPotency;
            glyphTime = this.stats.GetGearData().glyphTime;
            glyphProtection = this.stats.GetGearData().glyphProtection;

            spellCooldown = spellCooldownBase - (magickFragment * 1.5f);
            spellCooldown = Mathf.Clamp(spellCooldown, 6.0f, 30.0f);

            spellForceReloadSelfMul = spellForceReloadSelfMulBase - (glyphProtection * 0.15f);
            spellForceReloadSelfMul = Mathf.Clamp(spellForceReloadSelfMul, 0.05f, 1.0f);

            spellRange = spellRangeBase + (1.0f * glyphInfluence);

            spellForceReloadTimeAdd = spellForceReloadTimeAddBase + (0.5f * glyphPotency) + (0.25f * glyphTime);
            spellDuration = spellDurationBase + (0.25f * glyphPotency) + (0.5f * glyphTime);
        }

        internal void DeleteBullet()
        {
            float distance;
            List<GameObject> bulletToDelete = GameObject.FindGameObjectsWithTag("Bullet").ToList();
            foreach (GameObject bullet in bulletToDelete)
            {
                if (!bullet.name.Equals(bulletGameObjectName))
                    continue;

                distance = (bullet.transform.position - castPosition).magnitude;

                if (distance > spellRange)
                    continue;

                PhotonNetwork.Destroy(bullet);
            }
        }
        internal void DeleteBulletE()
        {
            float distance;
            List<GameObject> bulletToDelete = GameObject.FindGameObjectsWithTag("Bullet").ToList();
            foreach (GameObject bullet in bulletToDelete)
            {
                if (!bullet.name.Equals(bulletGameObjectName))
                    continue;

                distance = (bullet.transform.position - castPosEmpower).magnitude;

                if (distance > spellRange)
                    continue;

                PhotonNetwork.Destroy(bullet);
            }
        }

        internal void ApplyForceReload(Gun gun, GunAmmo gunAmmo, float duration)
        {
            Traverse.Create(gunAmmo).Field("currentAmmo").SetValue((int)0);
            Traverse.Create(gunAmmo).Field("freeReloadCounter").SetValue((float)0.0f);
            gunAmmo.InvokeMethod("SetActiveBullets", false);

            float reloadTime = (float)gunAmmo.InvokeMethod("ReloadTime") + duration;
            Traverse.Create(gunAmmo).Field("reloadCounter").SetValue((float) reloadTime);
            gun.isReloading = true;
            int maxAmmo = gunAmmo.maxAmmo;
            gun.player.data.stats.InvokeMethod("OnOutOfAmmp", maxAmmo);
        }

        private IEnumerator OnPointStart(IGameModeHandler gm)
        {
            effectWarmUp = true;
            // timeLastBlocked = Time.time - spellCooldown + warmupTime;
            CooldownTimer = warmupTime;
            spellReady = false;
            yield break;
        }

        private IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            effectWarmUp = false;
            // spellReady = true;

            yield break;
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            spellReady = false;
            empowerCharged = false;

            yield break;
        }

        public void OnDisable()
        {

        }

        public void OnDestroy()
        {
            // This effect should persist between rounds, and at 0 stack it should do nothing mechanically
            // UnityEngine.Debug.Log($"Destroying Scanner  [{this.player.playerID}]");

            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.RemoveHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);

            this.block.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Remove(this.block.BlockAction, this.spellAction);
            // UnityEngine.Debug.Log($"Scanner destroyed  [{this.player.playerID}]");
        }
    }
}
