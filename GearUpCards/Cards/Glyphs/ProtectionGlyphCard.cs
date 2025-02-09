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
using HarmonyLib;

namespace GearUpCards.Cards
{
    class ProtectionGlyphCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.typeGlyph
            };
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // block.cdAdd += 0.25f;

            // if (block.cdAdd > 0.25f)
            // {
            //     
            // }

            // float echoTime = (float) Traverse.Create(block).Field("timeBetweenBlocks").GetValue() + 0.05f;
            // echoTime = Mathf.Clamp(echoTime, 0.05f, 0.5f);
            // Traverse.Create(block).Field("timeBetweenBlocks").SetValue((float)echoTime);

            gun.attackSpeed += 0.1f;

            characterStats.GetGearData().glyphProtection += 1;

            GearUpPreRoundEffects mono = player.gameObject.GetOrAddComponent<GearUpPreRoundEffects>();
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // // undo stats where reset method doesn't clean up -- does not work well
            // float echoTime = (float)Traverse.Create(block).Field("timeBetweenBlocks").GetValue() - 0.05f;
            // echoTime = Mathf.Clamp(echoTime, 0.05f, 0.5f);
            // Traverse.Create(block).Field("timeBetweenBlocks").SetValue((float)echoTime);
        }
        protected override string GetTitle()
        {
            return "Protection Glyph";
        }
        protected override string GetDescription()
        {
            return "Improve block and reduce spell effect against you. Slow down block echoes.";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_GlyphProtection");
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Common;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat()
                {
                    positive = true,
                    stat = "Block I-Frame",
                    amount = "+35%",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Block CD",
                    amount = "-0.15s min",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "ATK Time",
                    amount = "+0.1s",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.MagicPink;
        }
        public override string GetModName()
        {
            return GearUpCards.ModInitials;
        }
        public override void Callback()
        {
            this.cardInfo.gameObject.AddComponent<ExtraName>().text = "Spell\nGlyph";
        }
    }
}
