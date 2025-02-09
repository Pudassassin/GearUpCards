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
    class GuardiansCharmCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false;
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.typeCharm,
                GearCategory.tagNoEternity
            };
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // black/whitelisting
            // ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.typeUniqueGunSpread);

            if (block.cdMultiplier > 1.0f)
            {
                block.cdMultiplier -= 0.35f;
            }
            else
            {
                block.cdMultiplier *= 0.65f;
            }

            if (block.cdAdd > 1.0f)
            {
                block.cdAdd -= 0.25f;
            }

            gunAmmo.reloadTimeAdd += 1.25f;
            // gun.attackSpeed *= 1.0f / 0.75f;

            // player.gameObject.GetOrAddComponent<GearUpPreRoundEffects>();
            characterStats.GetGearData().addOnList.Add(GearUpConstants.AddOnType.charmGuardian);
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {

        }
        protected override string GetTitle()
        {
            return "Guardians Charm";
        }
        protected override string GetDescription()
        {
            return "Boost your chance of drawing block-related cards.";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_GuardiansCharm");
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
                    stat = "Block CD",
                    amount = "-35% & -0.25s",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Reload Time",
                    amount = "+1.25s",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
                // new CardInfoStat()
                // {
                //     positive = false,
                //     stat = "ATK SPD",
                //     amount = "-25%",
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
            this.cardInfo.gameObject.AddComponent<ExtraName>().text = "Charm";
        }
    }
}
