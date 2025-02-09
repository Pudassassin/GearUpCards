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
using UnboundLib.Utils;

namespace GearUpCards.Cards
{
    class GeometricGlyphCard : CustomCard
    {
        private static string targetBounceCardName = "Target BOUNCE";
        public static GameObject screenEdgePrefab = null;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.typeGlyph
            };
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            gun.reflects += 3;
            // gunAmmo.reloadTimeMultiplier *= 1.10f;
            gunAmmo.reloadTimeAdd += 0.15f;

            characterStats.GetGearData().glyphGeometric += 1;

            // borrow modifier to bullet -- bounce off screen edge
            if (screenEdgePrefab == null)
            {
                CardInfo cardInfo = CardManager.cards.Values.First(card => card.cardInfo.cardName == targetBounceCardName).cardInfo;
                Gun cardGun = cardInfo.gameObject.GetComponent<Gun>();
                screenEdgePrefab = cardGun.objectsToSpawn[1].AddToProjectile;
            }

            List<ObjectsToSpawn> list = gun.objectsToSpawn.ToList<ObjectsToSpawn>();
            list.Add(new ObjectsToSpawn
            {
                AddToProjectile = screenEdgePrefab
            });

            gun.objectsToSpawn = list.ToArray();
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {

        }
        protected override string GetTitle()
        {
            return "Geometric Glyph";
        }
        protected override string GetDescription()
        {
            // return "Add some bounces to your Bullets and Spell Orbs. Not all Spells can be bouncy, through...";
            return "<i><b>\"Simple Trigonometry!\"</b></i>";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_GlyphGeometry");
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
                    stat = "Bullet Bounces",
                    amount = "+3",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Reload Time",
                    amount = "+0.15s",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = true,
                    stat = "Spell Bounces",
                    amount = "Increased",
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
