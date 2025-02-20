using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnboundLib.GameModes;
using Photon.Pun;
using UnboundLib;
using GearUpCards.Extensions;
using GearUpCards.Utils;

namespace GearUpCards.MonoBehaviours
{
    public class ArcaneDamageOvertimeEffect : MonoBehaviour
    {
        public static Color BlinkColor = new Color(0.55f, 0.1f, 0.55f, 0.3f);
        // static float ProcessRate = 0.05f;
        // internal float procTimer;

        // [Poison Bullet] is 3s with 0.3s tick time
        // [Parasite] do 5s with 0.3s tick time
        // [Decay] defaults to 4s

        private Player player;
        private CharacterData data;
        private HealthHandler healthHandler;

        public class ArcaneDoTStack
        {
            public float damageTotal, damageRemain, durationTotal, durationRemain, tickRate, tickTimer = 0.0f;
            public Color tickColor;

            public HealthHandler victimHH = null;
            public Player creditPlayer;
            public bool isLethal;

            public bool isActive = true;

            public ArcaneDoTStack(float damageTotal, float duration, float tickRate, HealthHandler victimHH, Color tickColor, Player creditPlayer = null, bool isLethal = true)
            {
                this.victimHH = victimHH;

                this.damageTotal = damageTotal;
                this.damageRemain = damageTotal;

                this.durationTotal = duration;
                this.durationRemain = duration;

                this.tickRate = tickRate;
                this.tickColor = tickColor;

                this.creditPlayer = creditPlayer;
                this.isLethal = isLethal;
            }

            public void Tick(float time, out float damageOut, out bool isLethal)
            {
                damageOut = 0.0f;
                isLethal = false;

                if (!isActive)
                {
                    return;
                }

                // process timer
                if (durationRemain >= tickRate && tickTimer >= tickRate)
                {
                    damageOut = damageRemain / durationRemain * tickRate;

                    damageRemain -= damageOut;
                    durationRemain -= tickRate;

                    tickTimer -= tickRate;
                }
                else if (durationRemain > 0.0f && durationRemain < tickRate && tickTimer > durationRemain)
                {
                    damageOut = damageRemain;

                    durationRemain = 0.0f;
                    damageRemain = 0.0f;
                    isActive = false;

                    tickTimer = 0.0f;
                }

                tickTimer += time;

                // check deal damage
                if (damageOut >= 0)
                {
                    // deal magic damage
                    victimHH.InvokeMethod("DisplayDamage", tickColor);
                    victimHH.Heal(damageOut * -1.0f);
                    isLethal = this.isLethal;

                    // resolve lifesteal, reaction, if any
                    if (creditPlayer != null)
                    {
                        Player victimPlayer = victimHH.gameObject.GetComponent<Player>();

                        // no lifesteal from oneself!
                        if (creditPlayer != victimPlayer)
                        {
                            CharacterStatModifiers creditorStats = creditPlayer.gameObject.GetComponent<CharacterStatModifiers>();
                            float effectiveLS = creditorStats.lifeSteal;

                            if (creditorStats.GetGearData().arcaneConversionStack > 0)
                            {
                                effectiveLS *= 0.5f;
                            }

                            creditPlayer.data.healthHandler.Heal(damageOut * effectiveLS);
                        }
                    }
                }

                // return damageOut;
            }

            public bool CheckActive()
            {
                return durationRemain > 0.0f && isActive;
            }
        }

        public List<ArcaneDoTStack> dotStack = new List<ArcaneDoTStack>();

        private bool effectEnabled;
        private bool wasDeactivated;

        private bool hasDamageDealt;
        private bool hasLethalDamage;
        private float damageDealt;

        public void Awake()
        {
            this.player = this.gameObject.GetComponent<Player>();
            this.data = this.player.data;
            this.healthHandler = data.healthHandler;

            // this.dotStack = new List<ArcaneDoTStack>();
            effectEnabled = true;

            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Update()
        {
            // Respawn case
            if (wasDeactivated)
            {
                OnRespawn();

                wasDeactivated = false;
                effectEnabled = true;
            }

            if (effectEnabled)
            {
                hasDamageDealt = false;
                hasLethalDamage = false;
                damageDealt = 0.0f;

                foreach (var stack in dotStack)
                {
                    stack.Tick(TimeHandler.deltaTime, out float damage, out bool lethal);

                    if (damage > 0.0f) 
                    {
                        hasDamageDealt = true;
                    }

                    damageDealt += damage;
                    hasLethalDamage = hasLethalDamage | lethal;
                }

                for (int i = dotStack.Count - 1; i >= 0; i--)
                {
                    if (!dotStack[i].CheckActive())
                    {
                        dotStack.RemoveAt(i);
                    }
                }

                // check for lethal and do killing blow
                if (data.health <= 0 && hasDamageDealt && hasLethalDamage && !data.dead)
                {
                    Miscs.KillOneLife(player);
                }
            }
        }

        public void ApplyNewStack(float damageTotal, float duration, float tickRate, Color tickColor, Player creditPlayer = null, bool isLethal = true)
        {
            ArcaneDoTStack stack = new ArcaneDoTStack(damageTotal, duration, tickRate, healthHandler, tickColor, creditPlayer, isLethal);
            dotStack.Add(stack);
        }

        public void OnRespawn()
        {
            dotStack.Clear();
        }

        public void OnRevive()
        {
            dotStack.Clear();
        }

        private IEnumerator OnPointStart(IGameModeHandler gm)
        {
            wasDeactivated = false;
            effectEnabled = false;

            OnRespawn();

            yield break;
        }

        private IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            effectEnabled = true;

            yield break;
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            dotStack.Clear();

            effectEnabled = false;

            yield break;
        }

        public void OnDisable()
        {
            bool isRespawning = player.data.healthHandler.isRespawning;
            // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting [{isRespawning}]");

            if (isRespawning)
            {
                OnRevive();
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting!?");
            }
            else
            {
                dotStack.Clear();

                wasDeactivated = true;
                effectEnabled = false;
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - dead ded!?");
            }
        }

        public void OnDestroy()
        {
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.RemoveHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }
    }
}
