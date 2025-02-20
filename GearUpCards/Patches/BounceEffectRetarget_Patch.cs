using System.Collections;
using System.Collections.Generic;

using HarmonyLib;

using UnityEngine;
using UnboundLib;

using GearUpCards.MonoBehaviours;
using GearUpCards.Utils;
using Photon.Pun;
using UnboundLib.Utils;

namespace GearUpCards.Patches
{
    [HarmonyPatch(typeof(BounceEffectRetarget))]
    class BounceEffectRetarget_Patch
    {
        public static float MaxBoundReflectAngle = 77.5f;
        public static float gravScale = 1.0f;

        // private static float constDisplacement = 0.1f;
        // private static float scaledDisplacement = 0.1f;
        // private static float constDisplaceFromVoid = 1.0f;

        // [HarmonyPrefix]
        // [HarmonyPriority(Priority.First)]
        // [HarmonyPatch("DoBounce")]
        // static void DoBouncePreFix(ref MoveTransform ___move, ref HitInfo hit)
        // {
        //     // relocate it so it didn't bury itself inside stuffs and/or void; it's already bounced and reflected
        //     // ___move.transform.position += ___move.velocity.normalized * (___move.velocity.magnitude * scaledDisplacement + constDisplacement);
        // 
        //     if (hit.transform == null)
        //     {
        //         Miscs.Log("void-normal: " + hit.normal);
        //         Miscs.Log("void-pos   : " + hit.point);
        //         Miscs.Log("position   : " + ___move.transform.position);
        //         ___move.transform.position += (Vector3)hit.normal * (___move.velocity.magnitude * scaledDisplacement + constDisplaceFromVoid);
        //         Miscs.Log("position2  : " + ___move.transform.position);
        //     }
        // }

        // fix dictionary key duplicate issue
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("Start")]
        static bool Start_Override(BounceEffectRetarget __instance, ref PhotonView ___view, ref MoveTransform ___move)
        {
            bool hasOtherCopy = false;
            for (int i = 0; i < __instance.transform.childCount; i++)
            {
                if (__instance.transform.GetChild(i) == __instance.transform)
                {
                    break;
                }
                if (__instance.transform.GetChild(i).gameObject.name.Contains("A_TargetBounce"))
                {
                    hasOtherCopy = true;
                    break;
                }
            }

            if (hasOtherCopy)
            {
                __instance.enabled = false;
                return false;
            }

            ___view = __instance.gameObject.GetComponentInParent<PhotonView>();
            ___move = __instance.gameObject.GetComponentInParent<MoveTransform>();

            ChildRPC childRPC = __instance.gameObject.GetComponentInParent<ChildRPC>();

            if (childRPC.childRPCsVector2.ContainsKey("TargetBounce"))
            {
                childRPC.childRPCsVector2.Remove("TargetBounce");
            }
            childRPC.childRPCsVector2.Add("TargetBounce", (Vector2 vector) => { __instance.InvokeMethod("SetNewVel", vector); });

            if (childRPC.childRPCsInt.ContainsKey("TargetBounceLine"))
            {
                childRPC.childRPCsInt.Remove("TargetBounceLine");
            }
            childRPC.childRPCsInt.Add("TargetBounceLine", (int input) => { __instance.InvokeMethod("DrawLineTo", input); });

            return false;
        }

        // priority target to NEAREST ALIVE AND VISIBLE target
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch("FindTarget")]
        static void FindTargetRework(BounceEffectRetarget __instance, MoveTransform ___move, ref Player __result, ref HitInfo hit)
        {
            // Miscs.Log("position3  : " + ___move.transform.position);
            List<Player> candidatePlayers = new List<Player>(PlayerManager.instance.players);
            Vector3 refPosition = ___move.transform.position + (Vector3)hit.normal * 0.25f;
            // Miscs.Log(refPosition);
        
            __result = null;
            float candidateDistance = Mathf.Infinity;
        
            if (hit.transform != null)
            {
                if (hit.transform.GetComponent<Player>())
                {
                    candidatePlayers.Remove(hit.transform.GetComponent<Player>());
                }
            }
        
            foreach (var player in candidatePlayers)
            {
                if (PlayerManager.instance.CanSeePlayer(refPosition, player).canSee &&
                    ModdingUtils.Utils.PlayerStatus.PlayerAliveAndSimulated(player) &&
                    !player.data.healthHandler.isRespawning)
                {
                    float distance = (refPosition - player.transform.position).magnitude;
                    if (__result == null)
                    {
                        __result = player;
                        candidateDistance = distance;
                    }
                    else if (distance < candidateDistance)
                    {
                        __result = player;
                        candidateDistance = distance;
                    }
                }
            }

            TargetBounce_PatchHelper helper = __instance.gameObject.GetOrAddComponent<TargetBounce_PatchHelper>();
            helper.lastHitInfo = hit;
            helper.targetPlayer = __result;
            // return false;
        }

        // trying to fix bound-reflected bullet from going into bound again
        public class TargetBounce_PatchHelper : MonoBehaviour
        {
            public HitInfo lastHitInfo = null;
            public Player targetPlayer = null;

            public void Clean()
            {
                lastHitInfo = null;
                targetPlayer = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch("DoBounce")]
        static void TrackHitInfo(HitInfo hit, BounceEffectRetarget __instance)
        {
            // TargetBounce_PatchHelper helper = __instance.gameObject.GetOrAddComponent<TargetBounce_PatchHelper>();
            // helper.lastHitInfo = hit;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("SetNewVel")]
        static bool SetNewVel_Correction(ref Vector2 newVel, BounceEffectRetarget __instance, MoveTransform ___move)
        {
            TargetBounce_PatchHelper helper = __instance.gameObject.GetOrAddComponent<TargetBounce_PatchHelper>();
            float reflectAngle;
            // Miscs.Log($"Input - " + newVel);

            if (helper.lastHitInfo != null)
            {
                // reflectAngle = Vector2.Angle(helper.lastHitInfo.normal, newVel);
                // Miscs.Log("\nda patch\n");
                // Miscs.Log("in velo    : " + newVel);
                // Miscs.Log("void-normal: " + helper.lastHitInfo.normal);
                // Miscs.Log("void-pos   : " + helper.lastHitInfo.point);
                // Miscs.Log("angle to norm: " + reflectAngle);

                if (helper.targetPlayer != null)
                {
                    reflectAngle = Vector2.Angle(helper.lastHitInfo.normal, newVel);
                    if (reflectAngle <= MaxBoundReflectAngle)
                    {
                        // Miscs.Log($"Good bounce! ({reflectAngle}) - " + newVel);
                    }
                    else
                    {
                        Vector2 fixVelocity = Vector3.RotateTowards(helper.lastHitInfo.normal, newVel, (MaxBoundReflectAngle) * Mathf.Deg2Rad, 0.0f);
                        fixVelocity = fixVelocity.normalized * ___move.velocity.magnitude;
                        newVel = fixVelocity;

                        reflectAngle = Vector2.Angle(helper.lastHitInfo.normal, newVel);
                        if (reflectAngle > MaxBoundReflectAngle)
                        {
                            fixVelocity = (helper.targetPlayer.transform.position - __instance.transform.position).normalized * newVel.magnitude;
                            newVel = fixVelocity;
                        }
                        // Miscs.Log($"Steep bounce! ({reflectAngle}) - " + newVel);
                    }
                }
                //{
                //    Vector2 towardTo = ((Vector2)helper.targetPlayer.transform.position) - ((Vector2)__instance.transform.position);
                //    Vector2 fixVelocity = towardTo.normalized * ___move.velocity.magnitude * ___move.multiplier;
                //    
                //    float gravity = ___move.gravity * ___move.multiplier * gravScale;
                //
                //    Vector2 vector = Miscs.BulletDropCorrection(towardTo, fixVelocity, gravity);
                //
                //    reflectAngle = Vector2.Angle(helper.lastHitInfo.normal, vector);
                //    if (reflectAngle <= MaxBoundReflectAngle)
                //    {
                //        newVel = vector;
                //        // fixVelocity = fixVelocity.normalized * ___move.velocity.magnitude;
                //
                //        Miscs.Log($"Good bounce! ({reflectAngle}) - " + newVel);
                //    }
                //    else
                //    {
                //        fixVelocity = Vector3.RotateTowards(helper.lastHitInfo.normal, vector, (MaxBoundReflectAngle) * Mathf.Deg2Rad, 0.0f);
                //        fixVelocity = fixVelocity.normalized * ___move.velocity.magnitude;
                //        newVel = fixVelocity;
                //
                //        Miscs.Log($"Steep bounce! ({reflectAngle}) - " + newVel + $" [{vector}]");
                //    }
                //
                //    // Miscs.Log("out velo   : " + newVel);
                //}
            }

            ___move.enabled = true;
            ___move.velocity = newVel;

            helper.Clean();

            return false;
        }
    }
}