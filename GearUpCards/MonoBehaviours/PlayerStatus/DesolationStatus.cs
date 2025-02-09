using GearUpCards.Utils;
using ModdingUtils.MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;

namespace GearUpCards.MonoBehaviours
{
    internal class DesolationStatus : ReversibleEffect
    {
        public static float DefaultBlockIFrame = 0.3f;
        public static Color DefaultShieldOnCD = new Color(0.1752f, 0.1912f, 0.2075f, 1f);
        public static Color DefaultShieldOffCD = new Color(0.8f, 0.8f, 0.8f, 1f);
        public static Color DefaultBlockPart = new Color(1.0f, 1.0f, 1.0f, 1f);

        // I-Frame duration multiplier to be used multiplicatively - 1.0f is no changes
        private float blockIFrameMul = 1.0f;

        // block cooldown "speed" multiplier to be used additively - 1.0f is "normal" cooldown speed
        private float blockCDSpeed = 1.0f;

        private float effectDuration = 4.0f;
        // private bool isFriendly = false;

        internal bool wasDisabled = false;

        internal float effectTimer = 0.0f;

        // visuals
        private GameObject shieldStoneObj = null;
        private SpriteRenderer shieldStoneSprite = null;
        private SetColorByBlockCD colorSetter = null;

        private GameObject shieldCDObj = null;
        private Image shieldCDImage = null;

        public void ApplyEffect(float IF_Factor, float CD_Factor, float duration)
        {
            effectTimer = 0.0f;
            if (IF_Factor < blockIFrameMul)
            {
                blockIFrameMul = IF_Factor;
            }

            if (CD_Factor < blockCDSpeed)
            {
                blockCDSpeed = CD_Factor;
            }

            if (duration > effectDuration)
            {
                effectDuration = duration;
            }

            // Update Status UI
            StatusUIMono statusUI = gameObject.GetOrAddComponent<StatusUIMono>();
            statusUI.SetIcon(StatusUIMono.StatusID.ArmorBreak, true);
        }

        public float GetBlockIFrameMultiplier()
        {
            return blockIFrameMul;
        }

        public float GetBlockCDMultiplier()
        {
            return blockCDSpeed;
        }

        public override void OnAwake()
        {
            // visual and VFX
            shieldStoneObj = Miscs.GetChildByHierachy(gameObject, "Limbs\\ArmStuff\\ShieldStone");
            colorSetter = shieldStoneObj.GetComponent<SetColorByBlockCD>();
            shieldStoneSprite = shieldStoneObj.GetComponent<SpriteRenderer>();

            shieldCDObj = Miscs.GetChildByHierachy(shieldStoneObj, "Canvas\\Image");
            shieldCDImage = shieldCDObj.GetComponent<Image>();

            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointEnd);

            var particleMain = block.particle.main;
            particleMain.startColor = new Color(1.0f, 0.4f, 0.4f);
        }

        public override void OnUpdate()
        {
            if (wasDisabled)
            {
                PurgeStatus();
            }

            effectTimer += TimeHandler.deltaTime;

            // effects
            if (block.IsOnCD())
            {
                float speedFactor = blockCDSpeed - 1.0f;
                block.counter += TimeHandler.deltaTime * speedFactor;
            }

            // visual and VFX
            if (blockIFrameMul <= 0.0f)
            {
                if (!block.IsOnCD())
                {
                    shieldStoneSprite.color = Color.red;
                }

                shieldCDImage.color = Color.red;
            }

            if (effectTimer > effectDuration)
            {
                PurgeStatus();
            }
        }

        private void PurgeStatus()
        {
            // clean up code

            // visual and VFX
            if (!block.IsOnCD())
            {
                shieldStoneSprite.color = DefaultShieldOffCD;
            }

            shieldCDImage.color = DefaultShieldOffCD;

            var particleMain = block.particle.main;
            particleMain.startColor = DefaultBlockPart;

            // Update Status UI
            StatusUIMono statusUI = gameObject.GetComponent<StatusUIMono>();
            statusUI.SetIcon(StatusUIMono.StatusID.ArmorBreak, false);

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
