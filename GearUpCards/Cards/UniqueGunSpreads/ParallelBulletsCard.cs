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
    class ParallelBulletsCard : CustomCard
    {
        public static GameObject objectToSpawn = null;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false;
            cardInfo.categories = new CardCategory[]
            {
                GearCategory.typeUniqueGunSpread,
                GearCategory.tagNoEternity
            };
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // black/whitelisting
            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.typeUniqueGunSpread);

            // gun.damage *= 0.35f;
            gun.bulletDamageMultiplier *= 0.35f;

            gun.spread += 0.50f;
            // gun.evenSpread += 1.0f;
            gun.numberOfProjectiles += 4;

            player.gameObject.GetOrAddComponent<UniqueGunSpreadMono>();
            characterStats.GetGearData().gunSpreadMod = GearUpConstants.ModType.gunSpreadParallel;

            // add modifier to bullet
            if (objectToSpawn == null)
            {
                objectToSpawn = new GameObject("ParallelBulletsModifier", new Type[]
                {
                    typeof(BulletNoClipModifier)
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
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(GearCategory.typeUniqueGunSpread);
        }
        protected override string GetTitle()
        {
            return "Parallel Bullets";
        }
        protected override string GetDescription()
        {
            return "Add and convert bullets spread into a parallel formation. Width scales with projectile counts.";
        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_ParallelBullets");
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
                    stat = "Projectiles",
                    amount = "+4",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "DMG",
                    amount = "-65%",
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
            this.cardInfo.gameObject.AddComponent<ExtraName>().text = "Unique\nSpread";
        }
    }
}
