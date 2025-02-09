using System.Collections.Generic;

using UnboundLib;
using UnityEngine;
using UnboundLib.GameModes;
using ModdingUtils.MonoBehaviours;

using GearUpCards.Utils;
using GearUpCards.Extensions;
using System.Collections;
using UnityEngine.UI;

namespace GearUpCards.MonoBehaviours
{
    internal class StatusUIMono : MonoBehaviour
    {
        private static GameObject UIPrefab = GearUpCards.VFXBundle.LoadAsset<GameObject>("UI_Status");
        private static float iconGapWidth = 0.5f;

        public enum StatusID
        {
            ArmorBreak,
            HealUp,
            HealDown
        }

        // public float _debugScale = 4.0f;

        private const float procTime = .10f;

        internal float timer = 0.0f;
        internal bool effectWarmup = false;
        internal bool effectEnabled = true;

        internal Player player;
        internal CharacterStatModifiers stats;

        internal GameObject statusUI = null;

        internal List<GameObject> iconList;

        internal GameObject armorBreakIcon = null;

        internal GameObject healUpIcon = null;

        internal GameObject healDownIcon = null;

        // internal float tempCooldown;
        private bool wasDeactivated = false;

        /* DEBUG */
        // internal int proc_count = 0;
        // bool skipOrbs = false;
        // bool skipBattery = false;


        public void Awake()
        {
            this.player = this.gameObject.GetComponent<Player>();
            this.stats = this.gameObject.GetComponent<CharacterStatModifiers>();

            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            // GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Start()
        {
            statusUI = Instantiate(UIPrefab);
            statusUI.transform.parent = player.transform;
            statusUI.transform.localPosition = Vector3.zero;

            statusUI.name = $"StatusUI_p({player.playerID})";
            statusUI.GetComponentInChildren<Canvas>().sortingLayerName = "MostFront";

            iconList = new List<GameObject>();

            armorBreakIcon = Miscs.GetChildByHierachy(statusUI, "Canvas\\Desolation");
            iconList.Add(armorBreakIcon);

            healUpIcon = Miscs.GetChildByHierachy(statusUI, "Canvas\\HealUp");
            iconList.Add(healUpIcon);

            healDownIcon = Miscs.GetChildByHierachy(statusUI, "Canvas\\HealDown");
            iconList.Add(healDownIcon);

            HideAllIcons();
        }

        public void Update()
        {
            if (wasDeactivated)
            {
                effectEnabled = true;
                wasDeactivated = false;
            }

            // statusUI.transform.position = player.transform.position;
            // statusUI.transform.localScale = Vector3.one * 3.0f;

            if (effectEnabled)
            {
                statusUI.SetActive(true);
            }
            else
            {
                statusUI.SetActive(false);
            }

        }

        internal string FormatCooldown(float cooldown)
        {
            if (cooldown >= 4.9f) return $"{cooldown:f0}";
            else return $"{cooldown:f1}";
        }

        public void RefreshIcon()
        {
            int statusCount = 0;

            foreach (var icon in iconList)
            {
                if (icon.activeSelf)
                {
                    statusCount++;
                }
            }

            if (statusCount > 0) 
            {
                float totalWidth = ((float)(statusCount - 1)) * iconGapWidth;
                int activeIndex = 0;

                foreach (var icon in iconList)
                {
                    if (icon.activeSelf)
                    {
                        icon.transform.localPosition = new Vector3((-totalWidth / 2.0f) + ((float)activeIndex) * iconGapWidth, 0.0f, 0.0f);
                        activeIndex++;
                    }
                }
            }
        }

        public void SetIcon(StatusID statusID, bool value)
        {
            this.ExecuteAfterFrames(1, () =>
            {
                switch (statusID)
                {
                    case StatusID.ArmorBreak:
                        armorBreakIcon.SetActive(value);
                        break;

                    case StatusID.HealUp:
                        healUpIcon.SetActive(value);
                        break;

                    case StatusID.HealDown:
                        healDownIcon.SetActive(value);
                        break;

                    default:
                        break;
                }

                RefreshIcon();
            });
        }

        public void HideAllIcons()
        {
            foreach (var icon in iconList)
            {
                icon.SetActive(false);
            }
        }

        private IEnumerator OnPointStart(IGameModeHandler gm)
        {
            //effectWarmup = true;
            effectEnabled = true;

            // FetchAbilities();

            yield break;
        }

        // private IEnumerator OnBattleStart(IGameModeHandler gm)
        // {
        //     effectWarmup = false;
        //     effectEnabled = true;
        // 
        //     // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - Battle Start");
        // 
        //     yield break;
        // }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            effectEnabled = false;
            // HideAllIcons();

            // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - Point End");
        
            yield break;
        }

        public void OnDisable()
        {
            bool isRespawning = player.data.healthHandler.isRespawning;
            // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting [{isRespawning}]");

            if (isRespawning)
            {
                // does nothing
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - is resurresting!?");
            }
            else
            {
                wasDeactivated = true;

                effectEnabled = false;
                statusUI.SetActive(false);
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - dead ded!?");
            }
        }

        public void OnDestroy()
        {
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointStart);
            // GameModeManager.RemoveHook(GameModeHooks.HookBattleStart, OnBattleStart);
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);

            Destroy(statusUI);
        }

    }
}
