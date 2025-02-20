using System;
using System.Collections.Generic;

using UnityEngine;

using GearUpCards.Utils;

namespace GearUpCards.MonoBehaviours.Visuals
{
    public class BulletVFXScale : MonoBehaviour
    {
        public static float LogBase = 1.8f;     // was 10.0f
        public static float MinScale = 0.5f;
        public static float MaxScale = 25.0f;
        public static float MinSize = 1.0f;
        public static float MaxSize = 10.0f;
        public static float XdmgShift = 2.5f;   // was 0.0f
        public static float YscaleShift = 0.0f; // was 2.0f
        public static float DivValue = 2.0f;    // was 2.0f

        public static int SparkChild = 3;
        public static float SparkScale = 0.65f;

        public static bool NormalizeStartingScale = true;

        public static int StartFromChild = 6;
        public int skipChild;

        public bool hasTargetBounce = false;

        public bool effectEnable = false;
        public List<Vector3> childrenScale = new List<Vector3>();

        private ProjectileHit projectileHit = null;
        private ArcaneConversionModifier arcaneMod = null;

        public void Setup()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                childrenScale.Add(transform.GetChild(i).localScale);

                if (NormalizeStartingScale)
                {
                    childrenScale[i] = Miscs.ScaleNormalize(childrenScale[i]);
                }

                // if (transform.GetChild(i).gameObject.name.Contains("A_TargetBounce"))
                // {
                //     if (hasTargetBounce)
                //     {
                //         transform.GetChild(i).gameObject.SetActive(false);
                //     }
                //     hasTargetBounce = true;
                // }
                if (transform.GetChild(i).gameObject.name.Contains("A_Thruster"))
                {
                    ParticleSystem.MainModule main = GetComponentInChildren<ParticleSystem>().main;
                    main.loop = true;
                    transform.GetChild(i).localScale = childrenScale[i];
                }
            }
            skipChild = transform.childCount - 1;
            childrenScale[SparkChild] = childrenScale[SparkChild] * SparkScale;

            projectileHit = gameObject.GetComponent<ProjectileHit>();
            arcaneMod = gameObject.GetComponentInChildren<ArcaneConversionModifier>();
        }

        public void Update()
        {
            if (!effectEnable)
            {
                MoveTransform moveTransform = gameObject.GetComponent<MoveTransform>();
                if (moveTransform != null && transform.childCount > StartFromChild)
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
                if (transform.childCount > childrenScale.Count)
                {
                    for (int i = childrenScale.Count; i < transform.childCount; i++)
                    {
                        childrenScale.Add(transform.GetChild(i).localScale);
                    }
                }

                try
                {
                    float damage = projectileHit.damage;
                    if (arcaneMod != null)
                    {
                        damage += arcaneMod.arcaneDamage;
                    }

                    float scale = (YscaleShift + Mathf.Log(XdmgShift + (damage / 55f), LogBase)) / DivValue;
                    scale = Mathf.Clamp(scale, MinScale, MaxScale);

                    for (int i = StartFromChild; i < transform.childCount; i++)
                    {
                        // if (i < StartFromChild && i != SparkChild) { continue; }
                        // if (i == skipChild) { continue; }
                        if (transform.GetChild(i).gameObject.name == "Trail" || transform.GetChild(i).gameObject.name == "Trail(Clone)")
                        {
                            transform.GetChild(i).localScale = childrenScale[i];
                            continue;
                        }
                        if (transform.GetChild(i).gameObject.name.Contains("E_BigBullet"))
                        {
                            transform.GetChild(i).localScale = childrenScale[i];
                            continue;
                        }
                        if (transform.GetChild(i).gameObject.name.Contains("A_Homing"))
                        {
                            transform.GetChild(i).localScale = childrenScale[i];
                            continue;
                        }

                        transform.GetChild(i).localScale = childrenScale[i] * scale;

                        if (transform.GetChild(i).localScale.x < MinSize)
                        {
                            transform.GetChild(i).localScale = Miscs.ScaleNormalize(transform.GetChild(i).localScale) * MinSize;
                        }
                        else if (transform.GetChild(i).localScale.x > MaxSize)
                        {
                            float excessScale = Mathf.Log(transform.GetChild(i).localScale.x, MaxSize) - 1.0f;
                            transform.GetChild(i).localScale = Miscs.ScaleNormalize(transform.GetChild(i).localScale) * (MaxSize + excessScale);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Miscs.LogWarn("[GearUp] BulletVFXScale encountered an error!");
                    Miscs.LogWarn(exception);
                    this.enabled = false;
                }
            }
        }
    }
}
