using System;
using UnityEngine;

using UnboundLib;

using GearUpCards.Extensions;
using GearUpCards.Utils;
using Photon.Pun;

namespace GearUpCards.MonoBehaviours
{
    internal class CustomEmpowerShotModifier : RayHitEffect
    {
        public static float procTickTime = 0.05f;
        public static string RPCKey = GearUpCards.ModId + ":CEmpowerImpactSync";

        public PhotonView view;

        private int empowerImpactCount = 1;
        private int empowerStackCount = 1;
        private float empowerBurstDelay = 0.1f;

        private Vector2 lastImpactPos;

        internal bool effectEnable = false;
        internal float procTimer = 0.0f;
        internal Vector3 prevBulletPos = Vector3.zero;

        private void Awake()
        {
            this.view = GetComponentInParent<PhotonView>();
            GetComponentInParent<ChildRPC>().childRPCsVector2.Add(RPCKey, SyncImpactPos);
        }

        public void SetupEmpowerCharge(int impact, int stack, float burstDelay = 0.1f)
        {
            empowerImpactCount = impact;
            empowerStackCount = stack;
            empowerBurstDelay = burstDelay;
        }

        public void SyncImpactPos(Vector2 pos)
        {
            prevBulletPos = pos;
        }

        // public void TempSetup()
        // {
        // // temp prototype
        //     ProjectileHit projectileHit = this.gameObject.GetComponentInParent<ProjectileHit>();
        //     Player shooterPlayer = projectileHit.ownPlayer;
        // 
        //     empowerImpactCount = shooterPlayer.data.stats.GetGearData().orbRollingBulwarkStack * 3;
        //     empowerStackCount = shooterPlayer.data.stats.GetGearData().orbRollingBulwarkStack;
        // }

        public void Update()
        {
            if (effectEnable)
            {
                procTimer += TimeHandler.deltaTime;
                if (procTimer >= procTickTime)
                {
                    prevBulletPos = transform.root.position;
                    procTimer -= procTickTime;
                }
            }
            else
            {
                MoveTransform moveTransform = GetComponentInParent<MoveTransform>();
                if (moveTransform != null)
                {
                    // TempSetup();
                    effectEnable = true;
                }
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            if (view != null && (view.IsMine))
            {
                GetComponentInParent<ChildRPC>().CallFunction(RPCKey, (Vector2)prevBulletPos);
            }

            if (empowerImpactCount <= 0)
            {
                return HasToReturn.canContinue;
            }

            lastImpactPos = prevBulletPos;
            if (lastImpactPos == Vector2.zero)
            {
                lastImpactPos = hit.point;
            }

            for (int i = 0; i < empowerStackCount; i++)
            {
                if (i == 0)
                {
                    GetComponentInParent<SpawnedAttack>().spawner.data.block.DoBlockAtPosition
                    (
                        firstBlock: true,
                        dontSetCD: true,
                        BlockTrigger.BlockTriggerType.Empower,
                        lastImpactPos - (Vector2)base.transform.forward * 0.05f,
                        onlyBlockEffects: true
                    );
                }
                else
                {
                    this.ExecuteAfterSeconds(empowerBurstDelay * (float)i, () =>
                    {
                        GetComponentInParent<SpawnedAttack>().spawner.data.block.DoBlockAtPosition
                        (
                            firstBlock: true,
                            dontSetCD: true,
                            BlockTrigger.BlockTriggerType.Empower,
                            lastImpactPos - (Vector2)base.transform.forward * 0.05f,
                            onlyBlockEffects: true
                        );
                    });
                }
            }

            empowerImpactCount--;
            if (empowerImpactCount <= 0)
            {
                CustomEmpowerVFX vfx = transform.root.GetComponentInChildren<CustomEmpowerVFX>();
                if (vfx != null)
                {
                    vfx.gameObject.SetActive(false);
                }
            }

            return HasToReturn.canContinue;
        }

        private void OnDestroy()
        {
            GetComponentInParent<ChildRPC>()?.childRPCsVector2.Remove(RPCKey);
        }
    }

    public class CustomEmpowerVFX : RayHitEffect
    {
        private static GameObject empowerShotVFX = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_EmpowerShot");
        public static float LogBase = 2.2f;
        public static float MinSize = 0.5f;
        public static float MaxSize = 25.0f;
        public static float VFXScale = 0.2f;

        ProjectileHit projectileHit;
        ArcaneConversionModifier arcaneMod;
        Player shooterPlayer;

        private GameObject vfxObject;
        private float bulletSize = 1.0f;
        private float damage;

        internal bool effectEnable = false;

        public void Setup()
        {
            projectileHit = this.gameObject.GetComponentInParent<ProjectileHit>();
            arcaneMod = this.gameObject.GetComponentInParent <ArcaneConversionModifier>();

            shooterPlayer = projectileHit.ownPlayer;

            vfxObject = UnityEngine.Object.Instantiate(empowerShotVFX, transform.root);
            vfxObject.transform.localEulerAngles = new Vector3(270.0f, 180.0f, 0.0f);
            vfxObject.transform.localScale = Vector3.one;
        }

        public void Update()
        {
            if (effectEnable)
            {
                damage = projectileHit.damage;
                if (arcaneMod != null)
                {
                    damage += arcaneMod.arcaneDamage;
                }
                bulletSize = VFXScale * (1.0f + Mathf.Log(damage / 55.0f, LogBase));
                bulletSize = Mathf.Clamp(bulletSize, MinSize, MaxSize);

                vfxObject.transform.localScale = Vector3.one * bulletSize;
            }
            else
            {
                MoveTransform moveTransform = GetComponentInParent<MoveTransform>();
                if (moveTransform != null)
                {
                    Setup();
                    effectEnable = true;
                }
            }
        }

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            if (hit.transform != null)
            {
                vfxObject.SetActive(false);
                this.enabled = false;
            }

            return HasToReturn.canContinue;
        }
    }
}
