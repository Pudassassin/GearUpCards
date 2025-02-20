using System.Collections;
using System.Linq;
using System.Collections.Generic;

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
using GearUpCards.Utils;
using GearUpCards.MonoBehaviours;

using UnboundLib.Utils;

using UnboundLib.Extensions;
using RarityLib.Utils;
using static GearUpCards.MonoBehaviours.CardDrawTracker;
using GearUpCards.MonoBehaviours.Visuals;

namespace GearUpCards
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.rayhitreflectpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.willuwontu.rounds.evenspreadpatch", BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency("root.rarity.lib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("root.category.util", BepInDependency.DependencyFlags.HardDependency)]

    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]

    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]

    public class GearUpCards : BaseUnityPlugin
    {
        public const string ModId = "com.pudassassin.rounds.GearUpCards";
        public const string ModName = "GearUpCards";
        public const string Version = "0.4.3.55"; //build #315  / Release 0-5-0

        public const string ModInitials = "GearUP";

        public const int CardEffectPriority = HarmonyLib.Priority.Last;
        public const int ExtraCardDrawPriority = HarmonyLib.Priority.Low;

        // // card category dicts >>> CardUtils.cs
        // public bool cardCategoryHasRarity = false;
        // public Dictionary<Rarity, CardCategory> rarityCategories = new Dictionary<Rarity, CardCategory>();
        // public Dictionary<string, CardCategory> packCategories = new Dictionary<string, CardCategory>();

        public static GameObject bulletObj;
        public static bool isCardPickingPhase = false;
        public static bool isCardExtraDrawPhase = false;
        static int lastPickerID = -1;

        // ========================================
        // base for mod configs
        
        public const string CompatibilityModName = "GearUpCards";

        internal static string ConfigKey(string name)
        {
            return $"{CompatibilityModName}_{name.ToLower()}";
        }
        internal static bool GetBool(string name, bool defaultValue = false)
        {
            return PlayerPrefs.GetInt(ConfigKey(name), defaultValue ? 1 : 0) == 1;
        }
        internal static void SetBool(string name, bool value)
        {
            PlayerPrefs.SetInt(ConfigKey(name), value ? 1 : 0);
        }
        internal static int GetInt(string name, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(ConfigKey(name), defaultValue);
        }
        internal static void SetInt(string name, int value)
        {
            PlayerPrefs.SetInt(ConfigKey(name), value);
        }
        internal static float GetFloat(string name, float defaultValue = 0)
        {
            return PlayerPrefs.GetFloat(ConfigKey(name), defaultValue);
        }
        internal static void SetFloat(string name, float value)
        {
            PlayerPrefs.SetFloat(ConfigKey(name), value);
        }


        public static bool ReducedVFX
        {
            get
            {
                return GetBool("ReduceVFX", true);
            }
            set
            {
                SetBool("ReduceVFX", value);
            }
        }
        public static bool EcoModeVFX
        {
            get
            {
                return GetBool("EcoModeVFX", true);
            }
            set
            {
                SetBool("EcoModeVFX", value);
            }
        }

        // ========================================

        void Awake()
        {
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            // gameObject.AddComponent<BountyDamageTracker>();
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
            CustomCard.BuildCard<DesolationCard>();
            CustomCard.BuildCard<LaserSightCard>();

            CustomCard.BuildCard<MedicCheckupCard>();
            CustomCard.BuildCard<HyperRegenerationCard>();

            CustomCard.BuildCard<ArcaneConversionCard>();

            // Bounty system card
            // CustomCard.BuildCard<SheriffCard>();

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
            CustomCard.BuildCard<MysticMissileCard>();

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
            CustomCard.BuildCard<ReplicationGlyphCard>();
            CustomCard.BuildCard<ProtectionGlyphCard>();

            // Booster Packs
            CustomCard.BuildCard<VeteransFriendCard>();
            CustomCard.BuildCard<VintageGearsCard>();
            CustomCard.BuildCard<SupplyDropCard>();

            // Shuffle Cards
            CustomCard.BuildCard<PureCanvasCard>();

            // Adding hooks
            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, PointEnd);

            // GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPickStart);
            // GameModeManager.AddHook(GameModeHooks.HookGameStart, OnPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, OnPlayerPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, PointStart);

            GameModeManager.AddHook(GameModeHooks.HookPickEnd, LateExtraDrawEvent, ExtraCardDrawPriority);

            // make cards mutually exclusive
            this.ExecuteAfterFrames(10, () =>
            {
                // temp exclusive (Known issues)
                MakeExclusive("Arcane Conversion", "Flak Cannon");

                // ==============

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
                MakeExclusive("Gatling Gun", "Flak Cannon");
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

                if (GetCardInfo("Pong") != null)
                {
                    CardInfo otherCard = GetCardInfo("Pong");
                    MakeExclusive("Pong", "Flak Cannon");
                    MakeExclusive("Pong", "Arc of Bullets");
                    MakeExclusive("Pong", "Parallel Bullets");

                    List<CardCategory> newList = otherCard.categories.ToList();
                    newList.Add(GearCategory.typeGunMod);
                    newList.Add(GearCategory.typeUniqueGunSpread);
                    otherCard.categories = newList.ToArray();
                }

                // [Flak Cannon] Vs CR
                MakeExclusive("Hive", "Flak Cannon");
                
                // [Flak Cannon] Vs Cards+
                MakeExclusive("Snake Attack", "Flak Cannon");

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

                // Vs RSCards & Classes
                if (GetCardInfo("Mirror Mage"))
                {
                    CardInfo otherCard = GetCardInfo("Mirror Mage");
                    MakeExclusive("Mirror Mage", "Flak Cannon");
                    MakeExclusive("Mirror Mage", "Arc of Bullets");
                    MakeExclusive("Mirror Mage", "Parallel Bullets");

                    List<CardCategory> newList = otherCard.categories.ToList();
                    newList.Add(GearCategory.typeGunMod);
                    newList.Add(GearCategory.typeUniqueGunSpread);
                    otherCard.categories = newList.ToArray();
                }

                // Vs other 'Piercing' damage card
                MakeExclusive("Arcane Conversion", "Piercing Bullets");
                MakeExclusive("Arcane Conversion", "Anonymity");

                // MakeExclusive("Arcane Conversion", "Gamer Ammunition");
                // MakeExclusive("Arcane Conversion", "Armor-Piercing Rounds");
                // MakeExclusive("Arcane Conversion", "Shadow Bullets");

            });

            // initialize card categories
            this.ExecuteAfterFrames(65, () =>
            {
                foreach (var item in RarityUtils.Rarities.Values)
                {
                    rarityCategories.Add(item, CustomCardCategories.instance.CardCategory("__Rarity-" + item.name));
                }
                foreach (var item in CardManager.categories)
                {
                    packCategories.Add(item, CustomCardCategories.instance.CardCategory("__Pack-" + item));
                }
            });

            // find and add mono to base object (careful!)
            this.ExecuteAfterFrames(95, () =>
            {
                GameObject[] gameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
                foreach (var item in gameObjects)
                {
                    if (item.name == "Bullet_Base" && item.CompareTag("Bullet"))
                    {
                        bulletObj = item;
                        break;
                    }
                }

                MoveTransform moveTransform = bulletObj.GetComponent<MoveTransform>();
                ProjectileHit projectileHit = bulletObj.GetComponent<ProjectileHit>();

                bool check = (moveTransform != null) && (projectileHit != null);

                if (check)
                {
                    BulletVFXScale scaler = bulletObj.AddComponent<BulletVFXScale>();
                    Miscs.Log("[GearUp] added experimental bullet VFX rescaler");
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
            // preping player's blacklist category
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

            // adding/clearing rarity in card category
            if (cardCategoryHasRarity)
            {
                // purge old rarity categories
                CardManager.cards.Values.ToList<Card>().ForEach(delegate (Card card)
                {
                    foreach (var item in rarityCategories.Values)
                    {
                        List<CardCategory> categoryList = new List<CardCategory>(card.cardInfo.categories);
                        if (categoryList.Contains(item))
                        {
                            categoryList.Remove(item);
                            CardCategory[] categories = categoryList.ToArray();
                            card.cardInfo.categories = categories;
                            break;
                        }
                    }
                });

                cardCategoryHasRarity = false;
            }

            CardManager.cards.Values.ToList<Card>().ForEach(delegate (Card card)
            {
                Rarity rarity = RarityUtils.GetRarityData(card.cardInfo.rarity);
                CardCategory rarityCat = rarityCategories[rarity];
                CardCategory[] cardCategories = card.cardInfo.categories.AddToArray(rarityCat);
                card.cardInfo.categories = cardCategories;
            });
            cardCategoryHasRarity = true;

            // MysticMissileCard origin object Clean Up
            foreach (var item in MysticMissileCard.objectSpawnDict.Values)
            {
                Destroy(item.effect);
                Destroy(item.AddToProjectile);
            }
            MysticMissileCard.objectSpawnDict.Clear();

            // preping essential mono to players (safe and sure way)
            foreach (var player in PlayerManager.instance.players)
            {
                player.gameObject.GetOrAddComponent<CardDrawTracker>();
            }

            GearUpPreRoundEffects.instanceList.Clear();

            yield break;
        }

        IEnumerator OnPickStart(IGameModeHandler gm)
        {
            Miscs.Log("\n[GearUp] OnPickStart()");
            // CardUtils.SaveCardRarity();
            isCardPickingPhase = true;

            yield break;
        }

        IEnumerator OnPlayerPickEnd(IGameModeHandler gm)
        {
            Miscs.Log("\n[GearUp] OnPickEnd()");
            CardUtils.RarityDelta.UndoAll();

            yield return new WaitForSecondsRealtime(0.1f);
            if (!isCardExtraDrawPhase && CardDrawTracker.extraDrawPlayerQueue.Count > 0)
            {
                isCardExtraDrawPhase = true;
                Miscs.Log("[GearUp] Extra draw locked in");

                for (int i = 0; i < CardDrawTracker.extraDrawPlayerQueue.Count; i++)
                {
                    Miscs.Log("Checking " + i + " | " + CardDrawTracker.extraDrawPlayerQueue[i]);
                    CardDrawTracker cardDrawTracker = CardDrawTracker.extraDrawPlayerQueue[i].gameObject.GetComponent<CardDrawTracker>();
            
                    if (cardDrawTracker != null)
                    {
                        yield return cardDrawTracker.ResolveExtraDraws();
                        yield return new WaitForSecondsRealtime(0.1f);
                    }
                    Miscs.Log("Checked! " + i + " | " + CardDrawTracker.extraDrawPlayerQueue[i]);
                }
            
                CardDrawTracker.extraDrawPlayerQueue.Clear();

                // requeue skipped draws
                if (CardDrawTracker.extraDrawPlayerQueue_Late.Count > 0)
                {
                    foreach (Player item in CardDrawTracker.extraDrawPlayerQueue_Late)
                    {
                        CardDrawTracker.extraDrawPlayerQueue.Add(item);
                    }
                    CardDrawTracker.extraDrawPlayerQueue_Late.Clear();
                }

                isCardExtraDrawPhase = false;
                Miscs.Log("[GearUp] Extra draw unlocked");
            }

            yield break;
        }

        IEnumerator LateExtraDrawEvent(IGameModeHandler gm)
        {
            Miscs.Log("\n[GearUp] LateExtraDrawEvent()");

            yield return new WaitForSecondsRealtime(0.1f);
            if (!isCardExtraDrawPhase && CardDrawTracker.extraDrawPlayerQueue.Count > 0)
            {
                isCardExtraDrawPhase = true;
                Miscs.Log("[GearUp] Late Extra draw locked in");

                for (int i = 0; i < CardDrawTracker.extraDrawPlayerQueue.Count; i++)
                {
                    Miscs.Log("Checking " + i + " | " + CardDrawTracker.extraDrawPlayerQueue[i]);
                    CardDrawTracker cardDrawTracker = CardDrawTracker.extraDrawPlayerQueue[i].gameObject.GetComponent<CardDrawTracker>();

                    if (cardDrawTracker != null)
                    {
                        yield return cardDrawTracker.ResolveExtraDraws(resolveLateDraw: true);
                        yield return new WaitForSecondsRealtime(0.1f);
                    }
                    Miscs.Log("Checked! " + i + " | " + CardDrawTracker.extraDrawPlayerQueue[i]);
                }

                CardDrawTracker.extraDrawPlayerQueue.Clear();
                isCardExtraDrawPhase = false;
                Miscs.Log("[GearUp] Late Extra draw unlocked");

            }

            // post card-phase code here
            GearUpPreRoundEffects.TriggerStatsMods();

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
        public static readonly AssetBundle ATPBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("gearup_game_effect", typeof(GearUpCards).Assembly);
        public static readonly AssetBundle VFXBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("gearup_asset", typeof(GearUpCards).Assembly);
        public static readonly AssetBundle CardArtBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("gearup_cardarts", typeof(GearUpCards).Assembly);

        // debug
        // public static void GetPlayerBlacklistedCards(Player player)
        // {
        //     return player.data.Get
        // }
    }
}
