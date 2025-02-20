using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnboundLib.GameModes;

using GearUpCards.Extensions;
using GearUpCards.Utils;
using System.Linq;

namespace GearUpCards.MonoBehaviours
{
    internal class LaserSightEffect : MonoBehaviour
    {
        public static int MaxLinePointCount = 300;
        public static float MinSpreadShow = 0.0f;
        public static float DefaultProjSpeed = 50.0f;
        public static float DefaultGravity = 100.0f;
        public static float PointDrawInterval = 0.005f;
        public static float EnemyLaserAlpha = 0.5f;

        public static float abilityRangeBase = 15f;
        public static float abilityTimeBase = 0.35f;

        public static List<string> blacklistCard = new List<string>()
        {
            "Lightsaber",           //CatsArmy's card
            // "Shock Blast",
            "The Dark Queen"        //Root's card
            // the sword class
        };

        public static float gravityScale = 1.2f;
        public static float velocityScale = 1.0f;
        public static float dragScale = 1.0f;

        public static float spreadLineExtend = 3.0f;
        public static float spreadScale = 85.0f;

        private static GameObject GearPrefab = GearUpCards.VFXBundle.LoadAsset<GameObject>("VFX_LaserSight");

        private const float procTime = 0.01f;

        internal Action<BlockTrigger.BlockTriggerType> abilityAction;

        // ===== Ability modifiers =====
        internal int abilityStack = 0;

        internal float abilityRange = 20f;
        internal float abilityTime = 0.20f;

        internal List<float> deltaTimeLog = new List<float>();
        internal int timeLogIndex = 0;
        private int timeLogMax = 10;

        internal List<Vector3>points = new List<Vector3>();
        private LineRenderer laserLine, spreadLineCW, spreadLineCCW;

        // internal vars
        internal float timer = 0.0f;

        internal bool wasDeactivated = false;
        internal bool effectEnabled = true;

        private GameObject gearObject;

        internal Player player;
        internal CharacterStatModifiers stats;
        internal Gun gun;
        internal Aim aim;

        internal float spreadDegree, gravity, playerScale, bulletDrag, bulletDragMin;
        internal Vector3 aimDirection, projVelocity, tempVelo;

        public void Awake()
        {
            player = gameObject.GetComponent<Player>();
            stats = gameObject.GetComponent<CharacterStatModifiers>();
            gun = gameObject.GetComponent<WeaponHandler>().gun;
            aim = gameObject.GetComponent<Aim>();

            // Gear Deco/Cooldown Part
            gearObject = Instantiate(GearPrefab, this.player.transform.position + new Vector3(0.0f, 0.0f, 100.0f), Quaternion.identity);
            gearObject.transform.localScale = this.transform.localScale + Vector3.one;
            gearObject.transform.parent = this.transform;
            gearObject.name = "Gear_LaserSight";
            gearObject.SetActive(false);
            // gearObject.GetComponent<Canvas>().sortingLayerName = "MostFront";
            // gearObject.GetComponent<Canvas>().sortingOrder = 10000;

            laserLine = gearObject.GetComponent<LineRenderer>();
            spreadLineCCW = gearObject.transform.GetChild(0).GetComponent<LineRenderer>();
            spreadLineCW = gearObject.transform.GetChild(1).GetComponent<LineRenderer>();

            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Start()
        {
            // to be implemented next update - player colored laser and faded other's lines
            // try
            // {
            //     int clientPlayerID = PlayerManager.instance.players.First(player => player.data.view.IsMine).playerID;
            //     if (player.playerID != clientPlayerID)
            //     {
            //         Gradient gradient = laserLine.colorGradient;
            //         gradient.alphaKeys[0] = new GradientAlphaKey(EnemyLaserAlpha, 0.0f);
            // 
            //         spreadLineCCW.gameObject.SetActive(false);
            //         spreadLineCW.gameObject.SetActive(false);
            //     }
            // }
            // catch (System.Exception exception)
            // {
            //     Miscs.LogWarn(exception);
            // }
        }

        public void Update()
        {
            RecalculateEffectStats();
            if (TimeHandler.deltaTime < TimeHandler.fixedDeltaTime)
            {
                deltaTimeLog.Add(TimeHandler.fixedDeltaTime);
            }
            else
            {
                deltaTimeLog.Add(TimeHandler.deltaTime);
            }
            if (deltaTimeLog.Count > timeLogMax)
            {
                deltaTimeLog.RemoveAt(0);
            }

            // Respawn case
            if (wasDeactivated)
            {
                wasDeactivated = false;
                effectEnabled = true;
            }

            if (effectEnabled && abilityStack > 0)
            {
                gearObject.SetActive(true);
                timer += TimeHandler.deltaTime;

                if (timer >= procTime)
                {
                    RefreshGunStats();

                    // calculate laser arc
                    points.Clear();
                    points.Add(gun.shootPosition.position);
                    // tempVelo = projVelocity;
                    MoveTransformSimulator moveSim = new MoveTransformSimulator(gun);
                    moveSim.velocity *= velocityScale;
                    moveSim.gravity *= gravityScale;
                    moveSim.drag *= dragScale;
                    moveSim.Update();
                    points.Add(moveSim.position);

                    // for (float t = PointDrawInterval; t <= abilityTime; t += PointDrawInterval)
                    // {
                    //     Vector3 thisPoint = points[points.Count - 1];
                    //     tempVelo += gravity * Vector3.down * PointDrawInterval;
                    //     
                    //     if ((tempVelo.magnitude > 2f) && tempVelo.magnitude > bulletDragMin)
                    //     {
                    //         tempVelo -= tempVelo * Mathf.Clamp(bulletDrag * PointDrawInterval, 0f, 1f);
                    //     }
                    //     
                    //     thisPoint += tempVelo;
                    //     
                    //     points.Add(thisPoint);
                    // }
                    float t = TimeHandler.deltaTime * 2.0f;
                    int loopCount = 0;
                    while (t < abilityTime && moveSim.distanceTravelled < abilityRange && loopCount < MaxLinePointCount)
                    {
                        moveSim.Update(deltaTimeLog[timeLogIndex]);
                        t += deltaTimeLog[timeLogIndex];

                        timeLogIndex++;
                        if (timeLogIndex >= deltaTimeLog.Count)
                        {
                            timeLogIndex = 0;
                        }

                        points.Add(moveSim.position);

                        loopCount++;
                    }

                    // int showUpTo = 0;
                    // for (int i = 0; i < points.Count; i++)
                    // {
                    //     if (Vector3.Distance(transform.position, points[i]) > abilityRange)
                    //     {
                    //         showUpTo = i - 1;
                    //         break;
                    //     }
                    // }
                    // 
                    // laserLine.numPositions = showUpTo;
                    // laserLine.SetPositions(points.GetRange(0, showUpTo).ToArray());

                    laserLine.numPositions = points.Count;
                    laserLine.SetPositions(points.ToArray());

                    // calculate spread indicator
                    // gonna implement better visuals
                    float spread = spreadDegree;
                    if (stats.GetGearData().gunSpreadMod == GearUpConstants.ModType.gunSpreadArc)
                    {
                        UniqueGunSpreadMono mono = gameObject.GetComponent<UniqueGunSpreadMono>();
                        spread = mono.prevSpread * mono.prevSpreadMul * 360.0f;
                    }
                    else if (stats.GetGearData().gunSpreadMod == GearUpConstants.ModType.gunSpreadArc)
                    {
                        spread = 0.0f;
                    }

                    if (Mathf.Abs(spread) > MinSpreadShow && Mathf.Abs(spread) < 360.0f)
                    {
                        spreadLineCCW.enabled = true;
                        spreadLineCW.enabled = true;

                        Vector3 tempAim = Miscs.RotateVector(aimDirection, spread / 2.0f);
                        Vector3 innerA = transform.position + tempAim * playerScale/2.0f;
                        Vector3 outerA = transform.position + tempAim * (playerScale/2.0f + spreadLineExtend);

                        tempAim = Miscs.RotateVector(aimDirection, -(spread) / 2.0f);
                        Vector3 innerB = transform.position + tempAim * playerScale/2.0f;
                        Vector3 outerB = transform.position + tempAim * (playerScale/2.0f + spreadLineExtend);

                        spreadLineCCW.numPositions = 2;
                        spreadLineCCW.SetPosition(0, innerA);
                        spreadLineCCW.SetPosition(1, outerA);
                        
                        spreadLineCW.numPositions = 2;
                        spreadLineCW.SetPosition(0, innerB);
                        spreadLineCW.SetPosition(1, outerB);
                    }
                    else
                    {
                        spreadLineCCW.enabled = false;
                        spreadLineCW.enabled = false;
                    }

                    timer -= procTime;
                }
            }
            else
            {
                gearObject.SetActive(false);
            }
        }

        private void RefreshGunStats()
        {
            aimDirection = gun.shootPosition.forward;
            aimDirection.z = 0.0f;
            aimDirection = aimDirection.normalized;
            projVelocity = aimDirection * DefaultProjSpeed * gun.projectileSpeed * velocityScale;

            spreadDegree = gun.spread * gun.multiplySpread * spreadScale;
            gravity = gun.gravity * DefaultGravity * gravityScale;
            bulletDrag = gun.drag * dragScale;
            bulletDragMin = gun.dragMinSpeed;

            playerScale = transform.localScale.x;
        }

        internal void RecalculateEffectStats()
        {
            abilityStack = stats.GetGearData().laserSightStack;

            abilityRange = abilityRangeBase * abilityStack;

            abilityTime = abilityTimeBase * abilityStack;
        }

        private IEnumerator OnPointStart(IGameModeHandler gm)
        {
            wasDeactivated = false;
            effectEnabled = true;
            yield break;
        }

        private IEnumerator OnBattleStart(IGameModeHandler gm)
        {

            yield break;
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            effectEnabled = false;
            yield break;
        }

        public void OnDisable()
        {
            bool isRespawning = player.data.healthHandler.isRespawning;
            // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting [{isRespawning}]");

            if (isRespawning)
            {
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting!?");
            }
            else
            {
                wasDeactivated = true;
                effectEnabled = false;
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - dead ded!?");
            }
        }

        public void OnDestroy()
        {
            // This effect should persist between rounds, and at 0 stack it should do nothing mechanically
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.RemoveHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }
    }
}
