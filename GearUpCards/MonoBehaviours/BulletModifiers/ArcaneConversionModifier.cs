using System;
using UnityEngine;

using UnboundLib;

using GearUpCards.Extensions;
using GearUpCards.Utils;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;

namespace GearUpCards.MonoBehaviours
{
    public class ArcaneConversionModifier : RayHitEffect
    {
        public static float ForceScale = 10.0f;

        private Player shooterPlayer = null;
        private GameObject shooterGunObj = null;

        private CharacterStatModifiers shooterStats = null;
        private ProjectileHit projectileHit = null;
        private HealthHandler shooterHealth = null;

        private int arcaneStack = 0;
        private RayHitPoison[] poisonStack = null;

        public float arcaneDamage = 0.0f;
        // private float arcaneHealing = 0.0f;
        public float arcaneLSMul = 0.0f;

        private bool effectEnable = false;
        private float prevDamage;
        private float damageDeltaMul;

        private float oldForce;

        public void Setup()
        {
            projectileHit = gameObject.GetComponentInParent<ProjectileHit>();
            shooterPlayer = projectileHit.ownPlayer;
            shooterGunObj = projectileHit.ownWeapon;

            shooterStats = shooterPlayer.gameObject.GetComponent<CharacterStatModifiers>();
            shooterHealth = shooterPlayer.gameObject.GetComponent<HealthHandler>();
            
            arcaneStack = shooterStats.GetGearData().arcaneConversionStack;

            if (arcaneStack > 0)
            {
                //arcaneDamage = projectileHit.damage * projectileHit.dealDamageMultiplierr;
                arcaneDamage = projectileHit.damage;

                if (arcaneStack == 1)
                {
                    arcaneDamage *= 0.5f;
                    projectileHit.damage *= 0.5f;

                    arcaneLSMul = 0.5f;
                }
                else if (arcaneStack >= 2)
                {
                    projectileHit.damage = 1.0f;

                    arcaneLSMul = 0.5f;
                }

                if (arcaneStack > 2)
                {
                    int delta = arcaneStack - 2;
                    arcaneDamage *= Mathf.Pow(1.35f, delta);
                }

                // if (shooterStats.lifeSteal > 0.0f)
                // {
                //     arcaneHealing = shooterStats.lifeSteal * arcaneLSMul * arcaneDamage;
                // }
            }

            prevDamage = projectileHit.damage;
            oldForce = projectileHit.force;

            // fix projectile's 'HP'
            ProjectileCollision pColl = transform.parent.GetComponentInChildren<ProjectileCollision>();
            pColl.health = projectileHit.damage + arcaneDamage;
            Traverse.Create(pColl).Field("startDMG").SetValue((float)(projectileHit.damage + arcaneDamage));
            Traverse.Create(pColl).Field("deathThreshold").SetValue((float)(pColl.health * 0.1f));
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
            else
            {
                if (prevDamage > 0.0f) 
                {
                    damageDeltaMul = projectileHit.damage / prevDamage;
                }
                else
                {
                    damageDeltaMul = 0.0f;
                }

                if (damageDeltaMul > 0.0f && damageDeltaMul < 10.0f)
                {
                    arcaneDamage *= damageDeltaMul;
                    damageDeltaMul = 1.0f;
                    prevDamage = projectileHit.damage;
                }

                if (arcaneStack > 0)
                {
                    projectileHit.force = oldForce + (arcaneDamage * ForceScale);
                }
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            Player targetPlayer = hit.transform.GetComponent<Player>();
            DamagableEvent damagableObject = hit.transform.GetComponent<DamagableEvent>();

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

            if (damagableObject != null && targetPlayer == null && arcaneStack > 0 && projectileHit.bulletCanDealDeamage)
            {
                // damagableObject.currentHP -= arcaneDamage;
                MoveTransform moveTransform = projectileHit.gameObject.GetComponent<MoveTransform>();
                damagableObject.CallTakeDamage(moveTransform.velocity.normalized * arcaneDamage, hit.point);
            }

            if (targetPlayer != null && arcaneStack > 0)
            {
                HealthHandler targetHealth = hit.transform.GetComponent<HealthHandler>();

                // protection / weakness against magic damage
                int protectionStack = targetPlayer.GetComponent<CharacterStatModifiers>().GetGearData().glyphProtection;
                float damageFactor = Mathf.Pow(0.9f, protectionStack);
                damageFactor = Mathf.Clamp(damageFactor, 0.2f, damageFactor);

                // workaround for [Poison] and [Parasite]
                poisonStack = transform.root.GetComponentsInChildren<RayHitPoison>();
                if (poisonStack.Length > 0)
                {
                    ArcaneDamageOvertimeEffect arcaneDOT = targetPlayer.gameObject.GetOrAddComponent<ArcaneDamageOvertimeEffect>();
                    float poisonMul = 1.0f / ((float)poisonStack.Length);
                    float damageBase = arcaneDamage * projectileHit.dealDamageMultiplierr * damageFactor;

                    foreach (var stack in poisonStack)
                    {
                        arcaneDOT.ApplyNewStack(
                            damageBase * poisonMul,
                            stack.time,
                            stack.interval,
                            arcaneStack < 2 ? stack.color : ArcaneDamageOvertimeEffect.BlinkColor,
                            shooterPlayer,
                            true
                        );
                    }
                }

                // check and execute whether bullet deal damage on contact
                if (projectileHit.bulletCanDealDeamage)
                {
                    targetPlayer.data.lastSourceOfDamage = null;

                    // deal magic damage, accounting magic resistance
                    float damageDealt = arcaneDamage * projectileHit.dealDamageMultiplierr * damageFactor;
                    targetHealth.Heal(damageDealt * -1.0f);

                    // faux 'lifesteal'
                    float arcaneHealing = shooterStats.lifeSteal * arcaneLSMul * damageDealt;
                    if (shooterStats.lifeSteal != 0.0f)
                    {
                        shooterHealth.Heal(arcaneHealing);
                    }

                    // check for lethal and do killing blow
                    if (targetPlayer.data.health <= 0 && !targetPlayer.data.dead)
                    {
                        Miscs.KillOneLife(targetPlayer);
                    }
                }
            }

            return HasToReturn.canContinue;
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }

    }
}
