﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.GameModes;
using UnityEngine;
using static GearUpCards.Utils.CardUtils;

namespace GearUpCards.MonoBehaviours
{
    internal class CardHandResolveMono : MonoBehaviour
    {
        private const float procTime = 1.0f;
        private const float resolveDelay = 0.5f;

        internal Player player;
        internal CharacterStatModifiers stats;

        internal float timer = 0.0f;
        internal float timeResolveCalled = 0.0f;
        internal bool manualResolve = false;

        /* DEBUG */
        // internal int proc_count = 0;


        public void Awake()
        {
            this.player = this.gameObject.GetComponent<Player>();
            this.stats = this.gameObject.GetComponent<CharacterStatModifiers>();

            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
            // GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Start()
        {

        }

        public void Update()
        {
            if (manualResolve && Time.time > timeResolveCalled + resolveDelay)
            {
                StartCoroutine(ResolveHandCards());
                manualResolve = false;
            }
        }

        private IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            ResolveHandCards();
            yield break;
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
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
                // UnityEngine.Debug.Log($"[HOLLOW] from player [{player.playerID}] - dead ded!?");
            }
        }
        public void OnDestroy()
        {
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnBattleStart);
            // GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        public void Refresh()
        {
            this.Awake();
        }

        public void TriggerResolve()
        {
            manualResolve = true;
            timeResolveCalled = Time.time;
        }

        public IEnumerator ResolveCardCategory(CardCategory category, string cardNameToAdd)
        {
            // Resolve card conflicts
            List<HandCardData> conflictedCards = GetPlayerCardsWithCategory(player, category);
            foreach (var item in conflictedCards)
            {
                UnityEngine.Debug.Log($"[{item.cardInfo.cardName}] - [{item.index}] - [{item.owner.playerID}]");
            }

            if (conflictedCards.Count >= 1)
            {
                var replacementCard = ModdingUtils.Utils.Cards.all.Where(card => card.name == cardNameToAdd).ToArray()[0];

                for (int i = conflictedCards.Count - 1; i >= 0; i--)
                {
                    ModdingUtils.Utils.Cards.instance.AddCardToPlayer
                    (
                        player, replacementCard, false, cardNameToAdd[..2], 0.0f, 0.0f
                    );

                    new WaitForSecondsRealtime(0.2f);
                    
                    ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer
                    (
                        player, conflictedCards[i].index, true
                    );

                    new WaitForSecondsRealtime(0.2f);

                    // ModdingUtils.Utils.Cards.instance.ReplaceCard
                    // (
                    //     player, conflictedCards[i].index, replacementCard, cardNameToAdd[..2], 2.0f, 2.0f, true
                    // );

                }
            }
            yield break;
        }

        internal void ResolveHandCards()
        {
            ResolveCardCategory(Category.typeSizeMod, "Medical Parts");
        }
    }
}