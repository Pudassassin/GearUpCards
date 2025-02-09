using System;
using System.Collections.Generic;
using System.Linq;

using UnboundLib;
using UnboundLib.Cards;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;

using UnityEngine;

using GearUpCards.MonoBehaviours;
using GearUpCards.Utils;
using GearUpCards.Extensions;
using static GearUpCards.Utils.CardUtils;

namespace GearUpCards.Cards
{
    class SizeNormalizerCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false;
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.typeSizeMod,
                GearCategory.tagNoRemove,
                GearCategory.tagNoEternity
            };
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Resolve card conflicts
            // CardHandResolveMono resolver = player.gameObject.GetOrAddComponent<CardHandResolveMono>();
            // List<HandCardData> conflictedCards = GetPlayerCardsWithCategory(player, GearCategory.typeSizeMod);
            // 
            // // foreach (var item in conflictedCards)
            // // {
            // //     UnityEngine.Debug.Log($"[{item.cardInfo.cardName}] - [{item.index}] - [{item.owner.playerID}]");
            // // }
            // 
            // if (conflictedCards.Count >= 1)
            // {
            //     resolver.TriggerResolve();
            //     return;
            // }

            // black/whitelisting
            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.typeSizeMod);

            // stats
            player.data.maxHealth *= 1.65f;
            characterStats.movementSpeed *= 1.35f;
            characterStats.GetGearData().sizeMod = GearUpConstants.ModType.sizeNormalize;

            // Add Size Normalizer mono
            // SizeNormalizerEffect effect = player.gameObject.GetOrAddComponent<SizeNormalizerEffect>();
            // effect.Refresh();
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // black/whitelisting here are too finicky
            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(GearCategory.typeSizeMod);
            // CardHandResolveMono resolver = player.gameObject.GetOrAddComponent<CardHandResolveMono>();
            // resolver.TriggerResolve();
        }
        protected override string GetTitle()
        {
            return "Size Normalizer";
        }
        protected override string GetDescription()
        {
            return "Set your final player size to default BUT leave your mass unchanged.";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_SizeNormalizer");
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat()
                {
                    positive = true,
                    stat = "HP",
                    amount = "+65%",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = true,
                    stat = "Movement SPD",
                    amount = "+35%",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.TechWhite;
        }
        public override string GetModName()
        {
            return GearUpCards.ModInitials;
        }
        public override void Callback()
        {
            this.cardInfo.gameObject.AddComponent<ExtraName>().text = "Unique\nSize";
        }
    }
}
