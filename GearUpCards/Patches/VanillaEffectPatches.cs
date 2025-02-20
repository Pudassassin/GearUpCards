using System;
using System.Collections.Generic;
using HarmonyLib;

using Sonigon;
using UnityEngine;

using UnboundLib;
using UnboundLib.Utils;

using GearUpCards.Utils;
using Sonigon.Internal;
using GearUpCards.Extensions;

namespace GearUpCards.Patches
{
    // Make [Lifestealer] aura do a proper CallTakeDamage (need testing on multiple targets)
    [HarmonyPatch(typeof(DealDamageToPlayer))]
    class DealDamageToPlayer_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("Go")]
        static bool Go_Override(
            DealDamageToPlayer __instance,
            ref Player ___target,
            CharacterData ___data,
            DealDamageToPlayer.TargetPlayer ___targetPlayer,
            SoundEvent ___soundDamage,
            float ___damage,
            bool ___lethal
            )
        {
            if (!___target)
            {
                ___target = ___data.player;
                if (___targetPlayer == DealDamageToPlayer.TargetPlayer.Other)
                {
                    ___target = PlayerManager.instance.GetOtherPlayer(___target);
                }
            }
            if (___soundDamage != null && ___target != null && ___target.data != null && ___target.data.isPlaying && !___target.data.dead && !___target.data.block.IsBlocking())
            {
                SoundManager.Instance.Play(___soundDamage, ___target.transform);
            }
            ___target.data.healthHandler.CallTakeDamage(___damage * Vector2.up, __instance.transform.position, null, ___data.player, ___lethal);

            return false;
        }
    }

    // Make [Shield Charge] ramming damage do a proper RPCA calls
    [HarmonyPatch(typeof(ShieldCharge))]
    class ShieldCharge_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("RPCA_Collide")]
        static bool RPCA_Collide_Override(Vector2 pos, Vector2 colDir, int playerID,
            ShieldCharge __instance,
            ref bool ___cancelForce,
            ParticleSystem ___hitPart,
            Vector3 ___dir,
            float ___damage,
            AttackLevel ___level,
            float ___damagePerLevel,
            CharacterData ___data,
            float ___knockBack,
            float ___knockBackPerLevel,
            float ___stopForce,
            float ___shake
            )
        {
            CharacterStatModifiers sourceStats = ___data.gameObject.GetComponent<CharacterStatModifiers>();
            int chargerArcane = sourceStats.GetGearData().arcaneConversionStack;

            Player targetPlayer;
            try
            {
                targetPlayer = (Player)PlayerManager.instance.InvokeMethod("GetPlayerWithID", playerID);
                {
                    if (targetPlayer == null)
                    {
                        Miscs.LogWarn("[GearUp] ShieldCharge_Patch.RPCA_Collide_Override get null result!");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Miscs.LogWarn("[GearUp] ShieldCharge_Patch.RPCA_Collide_Override failed!");
                Miscs.LogWarn(e);
                return true;
            }

            CharacterData targetData = targetPlayer.gameObject.GetComponentInParent<CharacterData>();
            if ((bool)targetData)
            {
                ___cancelForce = true;
                ___hitPart.transform.rotation = Quaternion.LookRotation(___dir);
                ___hitPart.Play();

                targetData.healthHandler.CallTakeDamage(___dir * (___damage + (float)___level.LevelsUp() * ___damagePerLevel), __instance.transform.position, damagingPlayer: ___data.player);
                
                // if (chargerArcane > 0)
                // {
                //     float normalDamage = (___damage + (float)___level.LevelsUp() * ___damagePerLevel);
                //     float arcaneDamage, arcaneLifesteal;
                //     Player creditPlayer = ___data.player;
                // 
                //     if (chargerArcane == 1)
                //     {
                //         arcaneDamage = normalDamage * 0.5f;
                //         normalDamage *= 0.5f;
                //         arcaneLifesteal = 0.5f;
                //     }
                //     else
                //     {
                //         arcaneDamage = normalDamage;
                //         normalDamage = 1.0f;
                //         arcaneLifesteal = 0.5f;
                //         creditPlayer = null;
                // 
                //         if (chargerArcane > 2)
                //         {
                //             int delta = chargerArcane - 2;
                //             arcaneDamage *= Mathf.Pow(1.35f, delta);
                //         }
                //     }
                // 
                //     if (sourceStats.lifeSteal != 0.0f)
                //     {
                //         float healing = sourceStats.lifeSteal * arcaneLifesteal * arcaneDamage;
                //         ___data.healthHandler.Heal(healing);
                //     }
                // 
                //     targetData.healthHandler.Heal(arcaneDamage * -1.0f);
                //     targetData.healthHandler.CallTakeDamage(___dir * normalDamage, __instance.transform.position, damagingPlayer: creditPlayer);
                //     Miscs.Log($"[GearUp] ShieldCharge_Patch.RPCA_Collide_Override:[{___data.player.playerID}] do arcane charge!!!");
                // }
                // else
                // {
                //     targetData.healthHandler.CallTakeDamage(___dir * (___damage + (float)___level.LevelsUp() * ___damagePerLevel), __instance.transform.position, damagingPlayer: ___data.player);
                // }
                
                targetData.healthHandler.CallTakeForce(___dir * (___knockBack + (float)___level.LevelsUp() * ___knockBackPerLevel));
                ___data.healthHandler.CallTakeForce(-___dir * ___knockBack, ForceMode2D.Impulse, false, true);
                ___data.healthHandler.CallTakeForce(-___dir * ___stopForce, ForceMode2D.Impulse, true, true);
                ___data.block.CallDoBlock(false, true, BlockTrigger.BlockTriggerType.ShieldCharge);
                GamefeelManager.GameFeel(___dir * ___shake);

                Miscs.Log($"[GearUp] ShieldCharge_Patch.RPCA_Collide_Override: [{___data.player.playerID}] ramming [{targetPlayer.playerID}]");
            }

            return false;
        }
    }

    // Make [Saw] effect do a proper RPCA calls & handle multiple targets
    [HarmonyPatch(typeof(Saw))]
    class Saw_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("Update")]
        static bool Update_Override(
            Saw __instance,
            Player ___owner,
            float ___range,
            float ___damage,
            float ___force,
            float ___shake,
            ParticleSystem[] ___parts,
            Transform ___sparkTransform,
            ref Vector3 ___forceDir
            )
        {
            List<Player> targetPlayers = new List<Player>();
            foreach (Player player in PlayerManager.instance.players)
            {
                if (player != ___owner && Vector3.Distance(player.transform.position, __instance.transform.position) < ___range * __instance.transform.localScale.x)
                {
                    targetPlayers.Add(player);
                }
            }

            Vector3 normalized = Vector3.up;

            foreach (Player target in targetPlayers)
            {
                if (PlayerManager.instance.CanSeePlayer(__instance.transform.position, target).canSee)
                {
                    normalized = (target.transform.position - __instance.transform.position).normalized;
                    if (___damage != 0f)
                    {
                        target.data.healthHandler.CallTakeDamage(TimeHandler.deltaTime * ___damage * normalized, __instance.transform.position, null, ___owner);
                    }
                    if (___force != 0f)
                    {
                        float num = Mathf.Clamp(1f - Vector2.Distance(__instance.transform.position, target.transform.position) / ___range, 0f, 1f);
                        ForceMultiplier component = target.GetComponent<ForceMultiplier>();
                        if ((bool)component)
                        {
                            num *= component.multiplier;
                        }
                        ___forceDir = normalized;
                        ___forceDir.y *= 0.5f;
                        //target.data.playerVel.AddForce(___forceDir * __instance.transform.localScale.x * num * TimeHandler.deltaTime * ___force, ForceMode2D.Force);
                        target.data.healthHandler.CallTakeForce(___forceDir * num * 0.001f * TimeHandler.deltaTime * ___force);
                    }
                }
            }

            // gonna patch VFX later...
            if (targetPlayers.Count > 0)
            {
                for (int j = 0; j < ___parts.Length; j++)
                {
                    if (!___parts[j].isPlaying)
                    {
                        ___parts[j].Play();
                    }
                }
                if ((bool)___sparkTransform)
                {
                    ___sparkTransform.transform.position = targetPlayers[targetPlayers.Count - 1].transform.position;
                    if (normalized != Vector3.zero)
                    {
                        ___sparkTransform.rotation = Quaternion.LookRotation(normalized);
                    }
                }
                GamefeelManager.GameFeel((normalized + UnityEngine.Random.onUnitSphere).normalized * ___shake * TimeHandler.deltaTime * 20f);
            }
            else
            {
                for (int k = 0; k < ___parts.Length; k++)
                {
                    if (___parts[k].isPlaying)
                    {
                        ___parts[k].Stop();
                    }
                }
            }

            return false;
        }
    }
}