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
    class ArcaneConversionCard : CustomCard
    {
        public static GameObject objectToSpawn = null;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.noType
            };
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // add ONLY one stack to the bullet modifier pool
            if (characterStats.GetGearData().arcaneConversionStack == 0)
            {
                if (objectToSpawn == null)
                {
                    objectToSpawn = new GameObject("ArcaneConversionModifier", new Type[]
                    {
                        typeof(ArcaneConversionModifier)
                    });
                    DontDestroyOnLoad(objectToSpawn);
                }

                List<ObjectsToSpawn> list = gun.objectsToSpawn.ToList<ObjectsToSpawn>();
                list.Add(new ObjectsToSpawn
                {
                    AddToProjectile = objectToSpawn
                });

                gun.objectsToSpawn = list.ToArray();
            }

            characterStats.GetGearData().arcaneConversionStack += 1;
            if (characterStats.GetGearData().arcaneConversionStack >= 2)
            {
                gun.unblockable = true;
            }
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {

        }
        protected override string GetTitle()
        {
            return "Arcane Conversion";
        }
        protected override string GetDescription()
        {
            return "Part of your damage magically bypass armors and reactions.\n2nd copy: Full convert & Unblockable!\nExtra copies gains:";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_ArcaneConversion");
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
                    stat = "\'DMG\' dealt",
                    amount = "+35% All",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "DMG taken",
                    amount = "+10% All",
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
            this.cardInfo.gameObject.AddComponent<ExtraName>().text = "Damage\nPassive";
        }
    }
}
