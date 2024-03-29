﻿using System.Collections;

using BepInEx;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using HarmonyLib;

using CardChoiceSpawnUniqueCardPatch.CustomCategories;

using UnityEngine;

using Jotunn;

using GearUpCards.Cards;
using static GearUpCards.Utils.CardUtils;
using UnboundLib.Utils;
using System.Linq;
using System.Collections.Generic;

using GearUpCards.Utils;
using UnboundLib.Extensions;

namespace GearUpCards
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.rayhitreflectpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.willuwontu.rounds.evenspreadpatch", BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency("root.rarity.lib", BepInDependency.DependencyFlags.HardDependency)]

    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]

    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]

    public class GearUpCards : BaseUnityPlugin
    {
        public const string ModId = "com.pudassassin.rounds.GearUpCards";
        public const string ModName = "GearUpCards";
        public const string Version = "0.3.1.2"; //build #214 / Release 0-3-0

        public const string ModInitials = "GearUP";

        // public static GearUpCards Instance { get; private set; }
        public static bool isCardPickingPhase = false;
        static int lastPickerID = -1;

        void Awake()
        {
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
        void Start()
        {
            // Instance = this;

            // Random idea cards
            CustomCard.BuildCard<HollowLifeCard>();
            CustomCard.BuildCard<ChompyBulletCard>();
            CustomCard.BuildCard<TacticalScannerCard>();
            CustomCard.BuildCard<SizeNormalizerCard>();
            CustomCard.BuildCard<BulletsDotRarCard>();

            // Tiberium card series
            CustomCard.BuildCard<TiberiumBulletCard>();

            // Unique Gun Spread
            CustomCard.BuildCard<ArcOfBulletsCard>();
            CustomCard.BuildCard<ParallelBulletsCard>();
            CustomCard.BuildCard<FlakCannonCard>();

            // Block Passives
            CustomCard.BuildCard<ShieldBatteryCard>();

            // Unique Magick series (powerful on-block "spell" abilities)
            CustomCard.BuildCard<AntiBulletMagickCard>();
            CustomCard.BuildCard<PortalMagickCard>();

            // Orb Spells
            CustomCard.BuildCard<ObliterationOrbCard>();
            CustomCard.BuildCard<RollingBulwarkOrbCard>();
            CustomCard.BuildCard<LifeforceDualityOrbCard>();
            CustomCard.BuildCard<LifeforceBlastOrbCard>();

            // Other Spells
            CustomCard.BuildCard<ArcaneSunCard>();

            // Specializing Charms
            CustomCard.BuildCard<GuardiansCharmCard>();

            // Spell Casting-Assistance-Device series
            CustomCard.BuildCard<GlyphCADModuleCard>();

            // Crystal card series

            // Parts/Material Passives
            CustomCard.BuildCard<GunPartsCard>();
            CustomCard.BuildCard<MedicalPartsCard>();

            // Spell Glyphs
            CustomCard.BuildCard<MagickFragmentsCard>();
            CustomCard.BuildCard<DivinationGlyphCard>();
            CustomCard.BuildCard<InfluenceGlyphCard>();
            CustomCard.BuildCard<GeometricGlyphCard>();
            CustomCard.BuildCard<PotencyGlyphCard>();
            CustomCard.BuildCard<TimeGlyphCard>();

            // Adding hooks
            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, PointEnd);

            // GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPickStart);
            // GameModeManager.AddHook(GameModeHooks.HookGameStart, OnPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, OnPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, PointStart);

            // make cards mutually exclusive
            this.ExecuteAfterFrames(10, () =>
            {
                if (GetCardInfo("Size Difference") != null)
                {
                    CardInfo otherCard = GetCardInfo("Size Difference");
                    MakeExclusive("Size Difference", "Size Normalizer");

                    List<CardCategory> newList = otherCard.categories.ToList();
                    newList.Add(GearCategory.typeSizeMod);
                    otherCard.categories = newList.ToArray();
                }
                if (GetCardInfo("Size Matters") != null)
                {
                    CardInfo otherCard = GetCardInfo("Size Matters");
                    MakeExclusive("Size Matters", "Size Normalizer");

                    List<CardCategory> newList = otherCard.categories.ToList();
                    newList.Add(GearCategory.typeSizeMod);
                    otherCard.categories = newList.ToArray();
                }

                // [Flak Cannon] Vs WWC
                if (GetCardInfo("Plasma Rifle") != null)
                {
                    CardInfo otherCard = GetCardInfo("Plasma Rifle");
                    MakeExclusive("Plasma Rifle", "Flak Cannon");

                    List<CardCategory> newList = otherCard.categories.ToList();
                    newList.Add(GearCategory.typeGunMod);
                    otherCard.categories = newList.ToArray();
                }
                if (GetCardInfo("Plasma Shotgun") != null)
                {
                    CardInfo otherCard = GetCardInfo("Plasma Shotgun");
                    MakeExclusive("Plasma Shotgun", "Flak Cannon");

                    List<CardCategory> newList = otherCard.categories.ToList();
                    newList.Add(GearCategory.typeGunMod);
                    otherCard.categories = newList.ToArray();
                }

                // [Flak Cannon] Vs PCE
                MakeExclusive("Fragmentation", "Flak Cannon");
                MakeExclusive("Fireworks", "Flak Cannon");

                // [Flak Cannon] Vs BSC
                MakeExclusive("Rolling Thunder", "Flak Cannon");
                MakeExclusive("Splitting Rounds", "Flak Cannon");

                // Vs CC's [Shock Blast]
                if (GetCardInfo("Shock Blast"))
                {
                    CardInfo otherCard = GetCardInfo("Shock Blast");
                    MakeExclusive("Shock Blast", "Flak Cannon");
                    MakeExclusive("Shock Blast", "Arc of Bullets");
                    MakeExclusive("Shock Blast", "Parallel Bullets");

                    List<CardCategory> newList = otherCard.categories.ToList();
                    newList.Add(GearCategory.typeGunMod);
                    newList.Add(GearCategory.typeUniqueGunSpread);
                    otherCard.categories = newList.ToArray();
                }
            });
        }

        void Update()
        {
            if (isCardPickingPhase)
            {
                if (lastPickerID != CardChoice.instance.pickrID)
                {
                    // CardUtils.RestoreGearUpCardRarity();

                    if (CardChoice.instance.pickrID >= 0)
                    {
                        CardUtils.ModifyPerPlayerCardRarity(CardChoice.instance.pickrID);
                    }

                    lastPickerID = CardChoice.instance.pickrID;
                }
            }

        }

        // initial card blacklist/whitelist at game start

        IEnumerator GameStart(IGameModeHandler gm)
        {
            foreach (var player in PlayerManager.instance.players)
            {
                // DONT DO THIS!!! ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Clear();

                // if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(GearCategory.typeCrystalMod))
                // {
                //     ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.typeCrystalMod);
                // }
                // 
                // if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(GearCategory.typeCrystal))
                // {
                //     ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.typeCrystal);
                // }

                if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(GearCategory.tagSpellOnlyAugment))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(GearCategory.tagSpellOnlyAugment);
                }

                if (ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(GearCategory.typeUniqueGunSpread))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(GearCategory.typeUniqueGunSpread);
                }

                if (ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(GearCategory.typeSizeMod))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(GearCategory.typeSizeMod);
                }

                if (ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(GearCategory.typeUniqueMagick))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Remove(GearCategory.typeUniqueMagick);
                }

                // player.gameObject.GetOrAddComponent<CardHandResolveMono>();
            }
            CardUtils.raritySnapshot = new Dictionary<string, float>();

            yield break;
        }

        IEnumerator OnPickStart(IGameModeHandler gm)
        {
            Miscs.Log("[GearUpCard] OnPickStart()");
            // CardUtils.SaveCardRarity();
            isCardPickingPhase = true;

            yield break;
        }

        IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            Miscs.Log("[GearUpCard] OnPickEnd()");
            // CardUtils.RestoreGearUpCardRarity();
            CardUtils.RarityDelta.UndoAll();
            // isCardPickingPhase = false;

            yield break;
        }

        // I'd love to have this redundancy running but it can make thing worse, leave it for later lel
        // IEnumerator CardPickEnd(IGameModeHandler gm)
        // {
        //     // UnityEngine.Debug.Log($"[GearUp Main] CardPickEnd Call");
        // 
        //     yield return new WaitForSecondsRealtime(.25f);
        // 
        //     foreach (var player in PlayerManager.instance.players)
        //     {
        //         // UnityEngine.Debug.Log($"[GearUp Main] Resolving player[{player.playerID}]");
        //         StartCoroutine(PlayerCardResolver.Resolve(player));
        //         yield return new WaitForSecondsRealtime(.1f);
        //     }
        // 
        //     yield break;
        // }

        IEnumerator PointEnd(IGameModeHandler gm)
        {
            MapUtils.ClearMapObjectsList();

            yield break;
        }

        IEnumerator PointStart(IGameModeHandler gm)
        {
            isCardPickingPhase = false;
        
            yield break;
        }

        // Assets loader
        public static readonly AssetBundle VFXBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("gearup_asset", typeof(GearUpCards).Assembly);
        public static readonly AssetBundle CardArtBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("gearup_cardarts", typeof(GearUpCards).Assembly);

        // debug
        // public static void GetPlayerBlacklistedCards(Player player)
        // {
        //     return player.data.Get
        // }
    }
}
