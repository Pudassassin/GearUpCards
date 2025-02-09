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
    class FlakCannonCard : CustomCard
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

            // gun.damage *= 0.65f;
            // 
            // // about half of [Buckshot]'s spread but this is for mono's calculation
            // gun.spread += 60.0f / 360.0f;
            // gun.evenSpread += 1.0f;
            // gun.numberOfProjectiles += 4;

            UniqueGunSpreadMono mono = player.gameObject.GetOrAddComponent<UniqueGunSpreadMono>();
            // mono.enabled = true;
            characterStats.GetGearData().gunSpreadMod = GearUpConstants.ModType.gunSpreadFlak;

            // add modifier to bullet
            if (objectToSpawn == null)
            {
                objectToSpawn = new GameObject("FlakCannonModifier", new Type[]
                {
                    // typeof(FlakShellModifier),
                    typeof(BulletSpeedLimiter),
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
            return "Flak Cannon";
        }
        protected override string GetDescription()
        {
            // return "Your gun fire bundled shells that split into shrapnels after 1s that also have bullet effects!"; //, or immediately after <i>directly hitting</i> someone.";
            return "Your gun fires <color=red>SLOWER and with FEWER</color> bullets that split into shrapnels after 1s.\n<color=green> Some of the shrapnels carries bullet effects!</color>";


        }
        protected override GameObject GetCardArt()
        {
            return GearUpCards.CardArtBundle.LoadAsset<GameObject>("C_FlakCannon");
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
                //     positive = false,
                //     stat = "Bursts",
                //     amount = "1/4 total",
                //     simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                // },
                // new CardInfoStat()
                // {
                //     positive = false,
                //     stat = "Shell Projs.",
                //     amount = "1/10 total",
                //     simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                // },
                // new CardInfoStat()
                // {
                //     positive = false,
                //     stat = "DMG",
                //     amount = "-35%",
                //     simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                // }
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
