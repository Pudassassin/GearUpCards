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
    class VeteransFriendCard : CustomCard
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
            // gun.damage *= 1.20f;
            // gun.attackSpeedMultiplier += 0.25f;
            // gunAmmo.maxAmmo += 3;

            CardDrawTracker cardDrawTracker = player.gameObject.GetOrAddComponent<CardDrawTracker>();

            CardDrawTracker.ExtraCardDraw extraCardDraw = new CardDrawTracker.ExtraCardDraw(1);
            Rarity rarity = CardUtils.TryQueryRarity("Exotic", "Rare");
            extraCardDraw.SetWhitelistCardPacks(new List<string> { "Vanilla" });
            extraCardDraw.SetWhitelistRarityRange(rarity, includeHigher: true);

            extraCardDraw.sourceCard = GetCardInfo("GearUP@Veterans Friend");

            cardDrawTracker.QueueExtraDraw(extraCardDraw);
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {

        }
        protected override string GetTitle()
        {
            return "Veterans Friend";
        }
        protected override string GetDescription()
        {
            return "You get to pick <color=green>ONE</color> <color=#FF2DC1ff>Rare</color> card from vanilla deck.\n(aka. <color=#0A45FFff>Exotic</color> or higher)";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_VeteransFriend");
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
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
