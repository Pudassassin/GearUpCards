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
    internal class DesolationEffect : MonoBehaviour
    {
        public static float _debugScale = 0.25f;

        private static GameObject VFXPrefab = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_Desolation");
        private static GameObject GearPrefab = GearUpCards.VFXBundle.LoadAsset<GameObject>("Gear_Desolation");

        private static float abilityCooldownBase = 6.5f;
        private static float abilityRangeBase = 4.5f;
        private static float abilityDurationBase = 2.0f;

        private static float abilityCooldownScaling = -0.5f;
        private static float abilityRangeScaling = 1.0f;
        private static float abilityDurationScaling = 1.0f;

        public float abilityCastDelay = 0.05f;
        public float abilityCastDelayTimer = 0.0f;
        public bool abilityIsCast = false;
        public BlockTrigger.BlockTriggerType abilityTrigger;

        private const float procTime = 0.01f;
        private const float warmupTime = 0.5f;

        internal Action<BlockTrigger.BlockTriggerType> abilityAction;

        // ===== Ability modifiers =====
        internal int desolationStack = 0;

        internal float abilityCooldown = 6.0f;
        internal float abilityRange = 6.0f;
        internal float abilityDuration = 3.0f;

        internal bool abilityReady = false;
        internal bool empowerCharged = false;

        internal Vector3 prevPosition;

        internal Vector3 castPosition;
        internal Vector3 castPosEmpower;

        // internal float timeLastBlocked = 0.0f;
        // internal float timeLastActivated = 0.0f;

        public float CooldownTimer = 0.0f;

        internal float timer = 0.0f;
        internal bool effectWarmUp = false;
        // internal int proc_count = 0;

        private GameObject gearObject;

        internal Player player;
        internal Block block;
        internal CharacterStatModifiers stats;


        public void Awake()
        {
            this.player = this.gameObject.GetComponent<Player>();
            this.block = this.gameObject.GetComponent<Block>();
            this.stats = this.gameObject.GetComponent<CharacterStatModifiers>();

            // Gear Deco/Cooldown Part
            gearObject = Instantiate(GearPrefab, this.player.transform.position + new Vector3(0.0f, 0.0f, 100.0f), Quaternion.identity);
            gearObject.transform.localScale = this.transform.localScale + Vector3.one * _debugScale;
            gearObject.transform.parent = this.transform;
            gearObject.name = "Gear_Desolation";
            gearObject.SetActive(false);
            // gearObject.GetComponent<Canvas>().sortingLayerName = "MostFront";
            // gearObject.GetComponent<Canvas>().sortingOrder = 10000;

            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Start()
        {
            // attach the new block effect and passing along reference to owner player's stats
            this.abilityAction = new Action<BlockTrigger.BlockTriggerType>(this.GetDoBlockAction(this.player, this.block).Invoke);
            this.block.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Combine(this.block.BlockAction, this.abilityAction);
        }

        public void Update()
        {
            timer += TimeHandler.deltaTime;
            if (CooldownTimer > 0.0f)
            { 
                CooldownTimer -= TimeHandler.deltaTime;
            }

            if (abilityIsCast)
            {
                if (abilityCastDelayTimer > 0.0f)
                {
                    abilityCastDelayTimer -= TimeHandler.deltaTime;
                }
                else
                {
                    abilityCastDelayTimer = 0.0f;
                    abilityIsCast = false;

                    CastAbility(player.transform.position, abilityTrigger);
                }
            }

            if (timer >= procTime)
            {
                prevPosition = this.player.transform.position;
                if (this.stats.GetGearData().desolationStack > 0)
                {
                    RecalculateEffectStats();
                    CheckReady();
                }
                else
                {
                    empowerCharged = false;
                    abilityReady = false;
                }

                timer -= procTime;
                // proc_count++;
            }

            // Gear visual update
            if (abilityReady)
            {
                gearObject.SetActive(true);
                gearObject.transform.localScale = Vector3.one * (1 + _debugScale);
            }
            else
            {
                gearObject.SetActive(false);
            }

            // this.stats.GetGearData().t_uniqueMagickCooldown = GetCooldown();
        }

        internal void CheckReady()
        {
            if (abilityReady)
            {
                return;
            }
            // else if (Time.time >= timeLastBlocked + spellCooldown)
            else if (CooldownTimer <= 0.0f)
            {
                abilityReady = true;
                CooldownTimer = 0.0f;
            }
        }

        public float GetCooldown()
        {
            // float cooldown = timeLastBlocked + spellCooldown - Time.time;
            if (abilityReady) return -1.0f;
            else return CooldownTimer;
        }

        // a work around the delegate limits, cheeky!
        public Action<BlockTrigger.BlockTriggerType> GetDoBlockAction(Player player, Block block)
        {
            return delegate (BlockTrigger.BlockTriggerType trigger)
            {
                bool conditionMet = (trigger == BlockTrigger.BlockTriggerType.Empower && empowerCharged) ||
                                    (trigger == BlockTrigger.BlockTriggerType.Default && abilityReady) ||
                                    (trigger == BlockTrigger.BlockTriggerType.ShieldCharge && abilityReady);

                if (conditionMet)
                {
                    // empower do cheeky teleport, I can just grab player.transform.position

                    if (trigger == BlockTrigger.BlockTriggerType.Empower)
                    {
                        castPosEmpower = this.player.transform.position;

                        empowerCharged = false;

                        CastAbility(castPosEmpower, trigger);
                        // timeLastActivated = Time.time;
                    }
                    else
                    {
                        castPosition = this.player.transform.position;

                        switch (trigger)
                        {
                            case BlockTrigger.BlockTriggerType.Default:
                                abilityCastDelayTimer = abilityCastDelay;
                                break;
                            case BlockTrigger.BlockTriggerType.ShieldCharge:
                                abilityCastDelayTimer = abilityCastDelay + 0.5f;
                                break;
                            default:
                                abilityCastDelayTimer = 0.0f;
                                break;
                        }
                        abilityIsCast = true;
                        abilityTrigger = trigger;

                        // timeLastBlocked = Time.time;
                        // timeLastActivated = Time.time;
                        CooldownTimer = abilityCooldown;
                        abilityReady = false;
                        empowerCharged = true;
                    }
                }
            };
        }

        public void CastAbility(Vector3 position, BlockTrigger.BlockTriggerType trigger)
        {
            // VFX Part
            GameObject VFX = Instantiate(VFXPrefab, this.player.transform.position + new Vector3(0.0f, 0.0f, 100.0f), Quaternion.identity);
            VFX.transform.localScale = Vector3.one * abilityRange;
            VFX.name = "DesolationVFX_Copy";
            VFX.GetComponent<Canvas>().sortingLayerName = "MostFront";
            VFX.GetComponent<Canvas>().sortingOrder = 10000;

            VFX.AddComponent<RemoveAfterSeconds>().seconds = 1.1f;

            // check players in range and apply status
            float distance;

            foreach (Player target in PlayerManager.instance.players)
            {
                // if (target.playerID == this.player.playerID && trigger == BlockTrigger.BlockTriggerType.Empower)
                // {
                //     // resolve empower position reversing its trickery
                //     distance = (this.prevPosition - position).magnitude;
                // }
                // else
                // {
                //     distance = (target.transform.position - position).magnitude;
                // }

                if (target.teamID == this.player.teamID)
                {
                    continue;
                }
                else
                {
                    distance = (target.transform.position - position).magnitude;
                }

                if (distance > abilityRange)
                    continue;

                // UnityEngine.Debug.Log($"[AntiBullet] Reading player[{target.playerID}]");

                DesolationStatus debuff = target.gameObject.GetOrAddComponent<DesolationStatus>();
                debuff.ApplyEffect(0.0f, 0.25f, abilityDuration);

                // UnityEngine.Debug.Log($"[AntiBullet] Forced-Reload player[{target.playerID}]");

            }
        }

        internal void RecalculateEffectStats()
        {
            desolationStack = this.stats.GetGearData().desolationStack;

            abilityCooldown = abilityCooldownBase + (abilityCooldownScaling * desolationStack);
            abilityCooldown = Mathf.Clamp(abilityCooldown, 2.5f, 30.0f);

            abilityRange = abilityRangeBase + (abilityRangeScaling * desolationStack) + this.transform.localScale.x;

            abilityDuration = abilityDurationBase + (abilityDurationScaling * desolationStack);
        }

        private IEnumerator OnPointStart(IGameModeHandler gm)
        {
            effectWarmUp = true;
            // timeLastBlocked = Time.time - spellCooldown + warmupTime;
            CooldownTimer = warmupTime;
            abilityReady = false;
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
            abilityReady = false;
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

            this.block.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Remove(this.block.BlockAction, this.abilityAction);
            // UnityEngine.Debug.Log($"Scanner destroyed  [{this.player.playerID}]");
        }
    }
}
