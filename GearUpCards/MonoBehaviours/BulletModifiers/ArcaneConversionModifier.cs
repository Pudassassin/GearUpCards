using System;
using UnityEngine;

using UnboundLib;

using GearUpCards.Extensions;
using GearUpCards.Utils;

namespace GearUpCards.MonoBehaviours
{
    public class ArcaneConversionModifier : RayHitEffect
    {
        private Player shooterPlayer = null;
        private GameObject shooterGunObj = null;

        private CharacterStatModifiers shooterStats = null;
        private ProjectileHit projectileHit = null;
        private HealthHandler shooterHealth = null;

        private int stackCount = 0;
        private float arcaneDamage = 0.0f;
        private float arcaneHealing = 0.0f;
        private float arcaneLSMul = 0.0f;

        private bool effectEnable = false;

        public void Setup()
        {
            projectileHit = gameObject.GetComponentInParent<ProjectileHit>();
            shooterPlayer = projectileHit.ownPlayer;
            shooterGunObj = projectileHit.ownWeapon;

            shooterStats = shooterPlayer.gameObject.GetComponent<CharacterStatModifiers>();
            shooterHealth = shooterPlayer.gameObject.GetComponent<HealthHandler>();
            
            stackCount = shooterStats.GetGearData().arcaneConversionStack;

            if (stackCount > 0)
            {
                arcaneDamage = projectileHit.damage * projectileHit.dealDamageMultiplierr;

                if (stackCount == 1)
                {
                    arcaneDamage *= 0.5f;
                    projectileHit.damage *= 0.5f;

                    arcaneLSMul = 0.5f;
                }
                else if (stackCount >= 2)
                {
                    if (projectileHit.damage > 1.0f)
                    {
                        projectileHit.damage = 1.0f;
                    }
                    else
                    {
                        projectileHit.damage *= 0.1f;
                    }

                    arcaneLSMul = 0.5f;
                }

                if (stackCount > 2)
                {
                    int delta = stackCount - 2;
                    arcaneDamage *= Mathf.Pow(1.25f, delta);
                }

                if (shooterStats.lifeSteal > 0.0f)
                {
                    arcaneHealing = shooterStats.lifeSteal * arcaneLSMul * arcaneDamage;
                }
            }
        }

        public void Update()
        {
            if (!effectEnable)
            {
                MoveTransform moveTransform = GetComponentInParent<MoveTransform>();
                if (moveTransform != null)
                {
                    try
                    {
                        Setup();
                    }
                    catch (Exception exception)
                    {
                        Miscs.LogWarn(exception);

                        this.enabled = false;
                        return;
                    }
                    effectEnable = true;
                }
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            // projectileHit.ownPlayer = shooterPlayer;
            // projectileHit.ownWeapon = shooterGunObj;

            if (hit.transform == null)
            {
                return HasToReturn.canContinue;
            }
            if (hit.transform.gameObject.tag.Contains("Bullet"))
            {
                return HasToReturn.canContinue;
            }

            if (hit.transform.GetComponent<Player>() && stackCount > 0)
            {
                // projectileHit.ownPlayer = null;
                // projectileHit.ownWeapon = null;
                Player targetPlayer = hit.transform.GetComponent<Player>();
                targetPlayer.data.lastSourceOfDamage = null;

                HealthHandler targetHealth = hit.transform.GetComponent<HealthHandler>();
                targetHealth.Heal(arcaneDamage * -1.0f);
                if (arcaneHealing > 0.0f)
                {
                    shooterHealth.Heal(arcaneHealing);
                }

                this.ExecuteAfterFrames(1, () =>
                {
                    targetHealth.RPCA_SendTakeDamage(new Vector2(1.0f, 0.0f), this.transform.position);
                });
            }

            return HasToReturn.canContinue;
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }

    }
}
