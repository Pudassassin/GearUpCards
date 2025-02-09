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
using RarityLib.Utils;

namespace GearUpCards.Cards
{
    class SupplyDropCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            ModdingUtils.Extensions.CardInfoExtension.GetAdditionalData(cardInfo).canBeReassigned = false;
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.typeBoosterPack,
                GearCategory.tagNoGlitch,
                GearCategory.tagNoRemove,
                GearCategory.tagNoTableFlip,
                GearCategory.tagCardManipulation,
                GearCategory.tagNoEternity
            };

            // gun.attackSpeed = 1.0f / 1.20f;
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            CardDrawTracker cardDrawTracker = player.gameObject.GetOrAddComponent<CardDrawTracker>();
            CardInfo thisCard = GetCardInfo("GearUP@Supply Drop!");

            // limiting one delivery at a time
            bool isDelivering = false;
            List<CardDrawTracker.ExtraCardDraw> drawQueues = new List<CardDrawTracker.ExtraCardDraw>(cardDrawTracker.extraCardDraws);
            drawQueues.AddRange(cardDrawTracker.extraCardDrawsDelayed);

            foreach (var queue in drawQueues)
            {
                if (queue.sourceCard == null) { continue; }
                if (queue.sourceCard.cardName == thisCard.cardName)
                {
                    isDelivering = true;
                    break;
                }
            }

            if (!isDelivering)
            {
                // black/whitelisting
                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.typeBoosterPack);
                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.tagCardManipulation);

                CardDrawTracker.ExtraCardDraw extraCardDraw = new CardDrawTracker.ExtraCardDraw(2, 1);
                Rarity rarity = TryQueryRarity("Uncommon", "Uncommon");
                extraCardDraw.SetWhitelistRarityRange(rarity, includeLower: true);
                extraCardDraw.isLateDraw = true;

                extraCardDraw.sourceCard = thisCard;
                extraCardDraw.dequeueAction = (player) =>
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(GearCategory.typeBoosterPack);
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(GearCategory.tagCardManipulation);
                };

                cardDrawTracker.QueueExtraDraw(extraCardDraw);
            }
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {

        }
        protected override string GetTitle()
        {
            return "Supply Drop!";
        }
        protected override string GetDescription()
        {
            return "You get to pick <color=green>TWO</color> more <color=#2CADFFff>Uncommon</color> or lower rarity cards in the next draw phase.\n<color=yellow>Only one ongoing supply drop per player at a time.</color>";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_SupplyDrop");
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                // new CardInfoStat()
                // {
                //     positive = true,
                //     stat = "DMG",
                //     amount = "+20%",
                //     simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                // },
                // new CardInfoStat()
                // {
                //     positive = true,
                //     stat = "ATK SPD",
                //     amount = "+20%",
                //     simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                // },
                // new CardInfoStat()
                // {
                //     positive = true,
                //     stat = "Max Ammo",
                //     amount = "+3",
                //     simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                // }
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
            this.cardInfo.gameObject.AddComponent<ExtraName>().text = "Booster\nPack";
        }
    }
}
