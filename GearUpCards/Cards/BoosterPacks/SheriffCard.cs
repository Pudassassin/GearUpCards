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
    class SheriffCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            ModdingUtils.Extensions.CardInfoExtension.GetAdditionalData(cardInfo).canBeReassigned = false;
            cardInfo.allowMultiple = false;
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.tagNoGlitch,
                GearCategory.tagNoRemove,
                GearCategory.tagNoTableFlip,
                // GearCategory.tagCardManipulation,
                GearCategory.uniqueCardSheriff,
                GearCategory.tagNoEternity
            };
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // only one Sheriff player in game
            foreach (Player gamePlayer in PlayerManager.instance.players)
            {
                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(gamePlayer.data.stats).blacklistedCategories.Add(GearCategory.uniqueCardSheriff);
            }
            BountyDamageTracker.hasSheriffBounty = true;

            // preparing the reward
            CardDrawTracker.ExtraCardDraw extraCardDraw = new CardDrawTracker.ExtraCardDraw(1);

            Rarity rarity = TryQueryRarity("Common", "Common");
            extraCardDraw.SetWhitelistRarityRange(rarity, includeLower: true);

            extraCardDraw.sourceCard = GetCardInfo("GearUP@Sheriff");
            BountyDamageTracker.sheriffBounty = extraCardDraw;

            // stats
            data.maxHealth *= 2.0f;
            gun.attackSpeed *= 1.0f / 1.50f;
            if (block.cdAdd <= 1.0f)
            {
                block.cdAdd *= 0.50f;
            }
            else
            {
                block.cdAdd -= 0.75f;
            }
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            foreach (Player gamePlayer in PlayerManager.instance.players)
            {
                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(gamePlayer.data.stats).blacklistedCategories.Remove(GearCategory.uniqueCardSheriff);
            }
            BountyDamageTracker.hasSheriffBounty = false;
        }
        protected override string GetTitle()
        {
            return "Sheriff";
        }
        protected override string GetDescription()
        {
            // return "<b>The one and only.</b>\nPlace a <b>common</b> card draw bounty on leading player each round- claim it by being the one who kill them the most!";
            return "Place a <b>Common</b> card bounty on leading player each round; claim it by killing them the most <b>AND</b> not letting them win!";
        }
        protected override GameObject GetCardArt()
        {
            // return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_PureCanvas");
            return null;
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat()
                {
                    positive = true,
                    stat = "Health",
                    amount = "+100%",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = true,
                    stat = "ATK SPD",
                    amount = "+50%",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = true,
                    stat = "Block CD",
                    amount = "-0.75s",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Bounty",
                    amount = "Indiscriminate",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.FirepowerYellow;
        }
        public override string GetModName()
        {
            return GearUpCards.ModInitials;
        }
        public override void Callback()
        {
            this.cardInfo.gameObject.AddComponent<ExtraName>().text = "ALPHA &\nOMEGA";
        }
    }
}
