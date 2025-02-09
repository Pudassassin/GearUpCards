using ModdingUtils.MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;

namespace GearUpCards.MonoBehaviours
{
    internal class LifeforceBlastStatus : ReversibleEffect
    {
        private static GameObject vfxHealBoost = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_Part_HealBoost");
        private static GameObject vfxHealHinder = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_Part_HealHinder");

        // value is the multiplier to be used multiplicatively
        private float healModifierFactor = 1.0f;
        private float effectDuration = 4.0f;
        private bool isFriendly = false;

        internal bool wasDisabled = false;

        internal float effectTimer = 0.0f;
        internal GameObject healBoostObj, healHinderObj;
        internal ParticleSystem healBoostPart, healHinderPart;
        ParticleSystem.ShapeModule shape;

        public void ApplyEffect(float factor, float duration, bool friendly)
        {
            if (friendly == this.isFriendly)
            {
                effectTimer = 0.0f;
                if (isFriendly && factor > healModifierFactor)
                {
                    healModifierFactor = factor;
                }
                else if (!isFriendly && factor < healModifierFactor)
                {
                    healModifierFactor = factor;
                }

                if (duration > effectDuration)
                {
                    effectDuration = duration;
                }
            }
            else
            {
                effectTimer = 0.0f;
                healModifierFactor = factor;
                effectDuration = duration;
                isFriendly = friendly;
            }

            // Update Status UI
            StatusUIMono statusUI = gameObject.GetOrAddComponent<StatusUIMono>();
            
            if (isFriendly)
            {
                statusUI.SetIcon(StatusUIMono.StatusID.HealUp, true);
                statusUI.SetIcon(StatusUIMono.StatusID.HealDown, false);
            }
            else
            {
                statusUI.SetIcon(StatusUIMono.StatusID.HealUp, false);
                statusUI.SetIcon(StatusUIMono.StatusID.HealDown, true);
            }
        }

        public float GetHealMultiplier()
        {
            return healModifierFactor;
        }

        public override void OnAwake()
        {
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointEnd);
        }

        public override void OnUpdate()
        {
            if (wasDisabled)
            {
                PurgeStatus();
            }

            effectTimer += TimeHandler.deltaTime;

            if (isFriendly)
            {
                if (healBoostObj == null)
                {
                    healBoostObj = Instantiate(vfxHealBoost, player.transform.root);
                    healBoostPart = healBoostObj.GetComponentInChildren<ParticleSystem>();
                }
                else
                {
                    healBoostObj.SetActive(true);
                }

                if (healHinderObj != null)
                {
                    healHinderObj.SetActive(false);
                }

                shape = healBoostPart.shape;
            }
            else
            {
                if (healHinderObj == null)
                {
                    healHinderObj = Instantiate(vfxHealHinder, player.transform.root);
                    healHinderPart = healHinderObj.GetComponentInChildren<ParticleSystem>();
                }
                else
                {
                    healHinderObj.SetActive(true);
                }

                if (healBoostObj != null)
                {
                    healBoostObj.SetActive(false);
                }

                shape = healHinderPart.shape;
            }

            shape.scale = Vector3.one + player.transform.localScale * 0.25f;


            if (effectTimer > effectDuration)
            {
                PurgeStatus();
            }
        }

        private void PurgeStatus()
        {
            // clean up code

            // visual and VFX
            if (healBoostObj != null)
            {
                Destroy(healBoostObj);
            }
            if (healHinderObj != null)
            {
                Destroy(healHinderObj);
            }

            // Update Status UI
            StatusUIMono statusUI = gameObject.GetComponent<StatusUIMono>();
            if (isFriendly)
            {
                statusUI.SetIcon(StatusUIMono.StatusID.HealUp, false);
            }
            else
            {
                statusUI.SetIcon(StatusUIMono.StatusID.HealDown, false);
            }

            // unhook events
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointEnd);

            Destroy(this);
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            // This status effect should be cleared out at point end, if they survived
            PurgeStatus();

            yield break;
        }

        override public void OnOnDisable()
        {
            // This status effect should be cleared out when they are dead, reviving or not
            wasDisabled = true;
            PurgeStatus();
        }
    }
}
