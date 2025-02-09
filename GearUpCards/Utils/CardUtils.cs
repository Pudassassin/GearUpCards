using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using HarmonyLib;

using UnboundLib;
using UnboundLib.Cards;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;

using UnityEngine;
using TMPro;
using RarityLib.Utils;
using GearUpCards.Extensions;
using UnboundLib.Utils;

namespace GearUpCards.Utils
{
    public class CardUtils
    {
        public static class GearCategory
        {
            public static CardCategory noType = CustomCardCategories.instance.CardCategory("GearUp_Unspecified");

            public static CardCategory uniqueCardSheriff = CustomCardCategories.instance.CardCategory("GearUp_Unique-Sheriff");

            public static CardCategory typeGunMod = CustomCardCategories.instance.CardCategory("GearUp_Gun-Mod");
            public static CardCategory typeBlockMod = CustomCardCategories.instance.CardCategory("GearUp_Block-Mod");
            public static CardCategory typeSizeMod = CustomCardCategories.instance.CardCategory("GearUp_Size-Mod");

            // public static CardCategory typeCrystal = CustomCardCategories.instance.CardCategory("GearUp_Crystal");
            // public static CardCategory typeCrystalGear = CustomCardCategories.instance.CardCategory("GearUp_Crystal-User");
            // public static CardCategory typeCrystalMod = CustomCardCategories.instance.CardCategory("GearUp_Crystal-Mod");

            public static CardCategory typeUniqueMagick = CustomCardCategories.instance.CardCategory("GearUp_Unique-Magick");
            public static CardCategory typeUniqueGunSpread = CustomCardCategories.instance.CardCategory("GearUp_Unique-Gun-Spread");
            public static CardCategory typeUniqueCAD = CustomCardCategories.instance.CardCategory("GearUp_Unique-CAD");

            public static CardCategory typeParts = CustomCardCategories.instance.CardCategory("GearUp_Parts");
            public static CardCategory typeCharm = CustomCardCategories.instance.CardCategory("GearUp_Charm");
            public static CardCategory typeGear = CustomCardCategories.instance.CardCategory("GearUp_Gear");

            public static CardCategory typeGlyph = CustomCardCategories.instance.CardCategory("GearUp_Glyph");
            public static CardCategory typeSpell = CustomCardCategories.instance.CardCategory("GearUp_Spell");
            public static CardCategory typeCadModule = CustomCardCategories.instance.CardCategory("GearUp_CAD-Module");

            public static CardCategory typeBoosterPack = CustomCardCategories.instance.CardCategory("GearUp_Booster-Pack");
            public static CardCategory typeCardShuffle = CustomCardCategories.instance.CardCategory("GearUp_Card-Shuffle");

            public static CardCategory tagCardManipulation = CustomCardCategories.instance.CardCategory("CardManipulation");
            public static CardCategory tagNoGlitch = CustomCardCategories.instance.CardCategory("NoRandom");
            public static CardCategory tagNoRemove = CustomCardCategories.instance.CardCategory("NoRemove");
            public static CardCategory tagNoTableFlip = CustomCardCategories.instance.CardCategory("NoFlip");
            public static CardCategory tagNoEternity = CustomCardCategories.instance.CardCategory("cantEternity");


            public static CardCategory tagSpellOnlyAugment = CustomCardCategories.instance.CardCategory("GearUp_Spell-Only-Augment");

            internal static List<CardCategory> __gearCategories = null;
            public static List<CardCategory> GearCategories
            {
                get
                {
                    if (__gearCategories == null)
                    {
                        __gearCategories = new List<CardCategory>()
                        {
                            noType,
                            typeBoosterPack,
                            typeCardShuffle,

                            typeGunMod,
                            typeBlockMod,
                            typeSizeMod,

                            typeUniqueMagick,
                            typeUniqueGunSpread,
                            typeUniqueCAD,

                            typeParts,
                            typeCharm,
                            typeGear,

                            typeGlyph,
                            typeSpell,
                            typeCadModule
                        };
                    }

                    return __gearCategories;
                }
            }
        }

        public class PlayerCardData
        {
            public CardInfo cardInfo;
            public Player owner;
            public int index;

            public PlayerCardData(CardInfo cardInfo, Player owner, int index)
            {
                this.cardInfo = cardInfo;
                this.owner = owner;
                this.index = index;
            }
        }

        // card category dicts
        public static bool cardCategoryHasRarity = false;
        public static Dictionary<Rarity, CardCategory> rarityCategories = new Dictionary<Rarity, CardCategory>();
        public static Dictionary<string, CardCategory> packCategories = new Dictionary<string, CardCategory>();

        public static Rarity TryQueryRarity(string query, string failSafe)
        {
            List<Rarity> rarityList = new List<Rarity>(rarityCategories.Keys);
            foreach (var item in rarityList)
            {
                if (item.name == query)
                {
                    return item;
                }
            }

            foreach (var item in rarityList)
            {
                if (item.name == failSafe)
                {
                    return item;
                }
            }

            return RarityUtils.GetRarityData(CardInfo.Rarity.Common);
        }

        public static bool PlayerHasCard(Player player, string cardName)
        {
            List<PlayerCardData> candidate = GetPlayerCardsWithName(player, cardName);
            return candidate.Count > 0;
        }

        public static bool PlayerHasCardCategory(Player player, CardCategory cardCategory)
        {
            List<PlayerCardData> candidate = GetPlayerCardsWithCategory(player, cardCategory);
            return candidate.Count > 0;
        }

        public static List<PlayerCardData> GetPlayerCardsWithName(Player player, string targetCardName)
        {
            targetCardName = targetCardName.ToUpper();
            string checkCardName;

            List<PlayerCardData> candidates = new List<PlayerCardData>();
            List<CardInfo> playerCards = player.data.currentCards;

            for (int i = 0; i < playerCards.Count; i++)
            {
                checkCardName = playerCards[i].cardName.ToUpper();

                if (checkCardName.Equals(targetCardName))
                {
                    candidates.Add(new PlayerCardData(playerCards[i], player, i));
                }
            }
            return candidates;
        }

        public static List<PlayerCardData> GetPlayerCardsWithCategory(Player player, CardCategory targetCategory)
        {
            List<PlayerCardData> candidates = new List<PlayerCardData>();
            List<CardInfo> playerCards = player.data.currentCards;

            for (int i = 0; i < playerCards.Count; i++)
            {
                bool match = false;
                CardCategory[] thisCardCat = CustomCardCategories.instance.GetCategoriesFromCard(playerCards[i]);
                foreach (CardCategory category in thisCardCat)
                {
                    if (targetCategory == category)
                    {
                        match = true;
                        break;
                    }
                }
                if (match)
                {
                    candidates.Add(new PlayerCardData(playerCards[i], player, i));
                }
            }
            return candidates;
        }

        public static CardInfo GetCardInfo(string modInitial, string cardNameExact)
        {
            string queryText = $"__{modInitial}__{cardNameExact}";
            List<CardInfo> cardInfoList = CardManager.cards.Values.Select(c => c.cardInfo).ToList();

            // Miscs.Log("GetCardInfo(exact) " + queryText);
            CardInfo result = null;
            foreach (CardInfo item in cardInfoList)
            {
                if (item.gameObject.name.Contains(queryText))
                {
                    result = item;
                    break;
                }
            }

            // if (result == null)
            // {
            //     Miscs.LogWarn("Cannot find: " + queryText);
            // }
            // else
            // {
            //     Miscs.Log("Found: " + result.name);
            // }
            return result;
        }

        public static CardInfo GetCardInfo(string query, bool searchVanillaOnly = false)
        {
            //would return the first hit in list of cards
            if (query.Contains("@"))
            {
                List<string> splitQuery = Miscs.StringSplit(query, '@');

                // foreach (var item in splitQuery)
                // {
                //     Miscs.Log("#> " + item);
                // }

                if (splitQuery.Count == 2)
                {
                    if (splitQuery[0] == "")
                    {
                        return GetCardInfo(splitQuery[1]);
                    }
                    else if (splitQuery[0] == "Vanilla")
                    {
                        return GetCardInfo(splitQuery[1], true);
                    }
                    return GetCardInfo(splitQuery[0], splitQuery[1]);
                }
                else
                {
                    Miscs.LogWarn(">> query splitting failed");
                    query = splitQuery[0];
                }
            }

            query = CardNameSanitize(query, removeWhitespaces: true);
            string cardName;
            List<CardInfo> cardInfoList = CardManager.cards.Values.Select(c => c.cardInfo).ToList();

            // if (!query.Contains("__"))
            // {
            //     searchVanillaOnly = true;
            // }

            // Miscs.Log("GetCardInfo(query) " + query);
            CardInfo result = null;
            int matchScore = 0;
            foreach (CardInfo item in cardInfoList)
            {
                cardName = CardNameSanitize(item.gameObject.name, removeWhitespaces: true);
                if (searchVanillaOnly && cardName.Contains("__")) continue;
                // Logging here is process time-intensive
                // Miscs.Log($"> Check for [{query}] <-> [{cardName}]");

                cardName = CardNameSanitize(item.cardName, removeWhitespaces: true);

                if (cardName.Equals(query))
                {
                    result = item;
                    matchScore = 9999;
                    break;
                }
                else if (cardName.Contains(query))
                {
                    int newMatch = Miscs.ValidateStringQuery(cardName, query);
                    if (newMatch > matchScore)
                    {
                        matchScore = newMatch;
                        result = item;
                    }
                }
            }

            // if (result == null)
            // {
            //     Miscs.LogWarn("Cannot find: " + query);
            // }
            // else
            // {
            //     Miscs.Log($"Found: {result.gameObject.name} [{matchScore}]");
            // }
            return result;
        }

        public static List<PlayerCardData> GetPlayerCardsWithCardInfo(Player player, CardInfo cardInfo)
        {
            string targetCardName = cardInfo.gameObject.name.ToUpper();
            string checkCardName;

            List<PlayerCardData> candidates = new List<PlayerCardData>();
            List<CardInfo> playerCards = player.data.currentCards;

            for (int i = 0; i < playerCards.Count; i++)
            {
                checkCardName = playerCards[i].gameObject.name.ToUpper();

                if (checkCardName.Equals(targetCardName))
                {
                    candidates.Add(new PlayerCardData(playerCards[i], player, i));
                }
            }
            return candidates;
        }

        public static List<PlayerCardData> GetPlayerCardsWithStringList(Player player, List<string> checkList)
        {
            List<PlayerCardData> candidates = new List<PlayerCardData>();
            List<CardInfo> playerCards = player.data.currentCards;
            CardInfo tempCardInfo;

            foreach (string item in checkList)
            {
                tempCardInfo = GetCardInfo(item);
                if (tempCardInfo == null) continue;

                candidates = candidates.Concat(GetPlayerCardsWithCardInfo(player, tempCardInfo)).ToList();
            }

            return candidates;
        }

        // adjusting per-player card rarity modifiers
        public class RarityDelta
        {
            static List<RarityDelta> rarityDeltas = new List<RarityDelta>();
            CardInfo cardInfo;
            float addDelta = 0.0f;
            float mulDelta = 0.0f;

            RarityDelta(CardInfo cardInfo, float add = 0.0f, float mul = 0.0f)
            {
                this.cardInfo = cardInfo;
                this.addDelta = add;
                this.mulDelta = mul;
            }

            void Undo()
            {
                if (cardInfo != null)
                {
                    RarityUtils.AjustCardRarityModifier(cardInfo, -1.0f * addDelta, -1.0f * mulDelta);
                    addDelta = 0.0f;
                    mulDelta = 0.0f;
                }
            }

            public static void AdjustRarityModifier(CardInfo cardInfo, float add = 0.0f, float mul = 0.0f)
            {
                // Miscs.Log("[GearUp] AdjustRarityModifier()");
                if (cardInfo == null) return;

                // Miscs.Log("[GearUp] AdjustRarityModifier(): A");
                RarityUtils.AjustCardRarityModifier(cardInfo, add, mul);

                // Miscs.Log("[GearUp] AdjustRarityModifier(): B");
                RarityDelta targetCard = null;
                foreach (RarityDelta item in rarityDeltas)
                {
                    if (item.cardInfo == cardInfo)
                    {
                        targetCard = item;
                        break;
                    }
                }

                // Miscs.Log("[GearUp] AdjustRarityModifier(): C");
                if (targetCard == null)
                {
                    rarityDeltas.Add(new RarityDelta(cardInfo, add, mul));
                }
                else
                {
                    targetCard.addDelta += add;
                    targetCard.mulDelta += mul;
                }
            }

            public static void UndoAll()
            {
                // Miscs.Log("[GearUp] UndoAll()");
                if (rarityDeltas == null) return;

                foreach (RarityDelta item in rarityDeltas)
                {
                    item.Undo();
                }

                // Miscs.Log("[GearUp] UndoAll(): A");
                rarityDeltas.Clear();
            }
        }

        public static List<string> gearUpRarityChecklist = new List<string>()
        {
            // spells
            "Anti-Bullet Magick",

            "Orb-literation!",
            "Rolling Borbwark",
            "Lifeforce Duorbity",
            "Lifeforce Blast!",

            "Arcane Sun",
            "Mystic Missile!",

            // glyphs
            "Divination Glyph",
            "Geometric Glyph",
            "Influence Glyph",
            "Magick Fragments",
            "Potency Glyph",
            "Time Glyph",
            "Replication Glyph",
            "Protection Glyph",

            // C&C cards
            "Tiberium Bullet",

            // others
            "Glyph CAD Module",
            "Shield Battery",
            "Pure Canvas"
        };
        public static Dictionary<string, float> raritySnapshot = new Dictionary<string, float>();

        public static List<string> cardListSpells = new List<string>()
        {
            // GearUp spells
            "Anti-Bullet Magick",

            "Orb-literation!",
            "Rolling Borbwark",
            "Lifeforce Duorbity",
            "Lifeforce Blast!",

            "Arcane Sun",
            "Mystic Missile!"
        };
        public static List<string> cardListGlyph = new List<string>()
        {
            // GearUp glyphs
            "Divination Glyph",
            "Geometric Glyph",
            "Influence Glyph",
            "Magick Fragments",
            "Potency Glyph",
            "Time Glyph",
            "Replication Glyph",
            "Protection Glyph"
        };

        public static List<string> cardListVanillaBlocks = new List<string>()
        {
            "Vanilla@Empower",

            "Vanilla@Bombs Away",
            "Vanilla@Defender",
            "Vanilla@EMP",
            "Vanilla@Frost Slam",
            "Vanilla@Healing Field",
            "Vanilla@Implode",
            "Vanilla@Overpower",
            "Vanilla@Radar Shot",
            "Vanilla@Saw",
            "Vanilla@Shield Charge",
            "Vanilla@Shockwave",
            "Vanilla@Silence",
            "Vanilla@Static Field",
            "Vanilla@Supernova",
            "Vanilla@Teleport"
        };
        public static List<string> cardListModdedBlocks = new List<string>()
        {
            // GearUp Cards - GearUP
            "GearUP@Magick Fragments",
            "GearUP@Shield Battery",
            "GearUP@Tactical Scanner",
            "GearUP@Desolation",
            "GearUP@Rolling Borbwark",
            "GearUP@Protection Glyph",

            // Willis' Cards Plus - Cards+
            "Cards+@Turtle",

            // Cosmic Rounds - CR
            "CR@Aqua Ring",
            "CR@Barrier",
            "CR@Gravity",
            "CR@Halo",
            "CR@Heartition",
            "CR@Hive",
            "CR@Holster",
            "CR@Ignite",
            "CR@Mitosis",
            "CR@Ping",
            "CR@Speed Up",
            //* "Taser",

            // HatchetDaddy's Cards - HDC
            "HDC@Divine Blessing",
            "HDC@Lil Defensive",

            // Root's Cards - Root
            "Root@Quick Shield",

            // Pykess's Cards Extended - PCE
            "PCE@Discombobulate",
            "PCE@Super Jump"
        };

        public static void SaveCardRarity()
        {
            Miscs.Log("[GearUp] SaveGearUpCardRarity()");
            CardInfo tempCardInfo;
            string tempCardName;

            // Miscs.Log("[GearUp] SaveGearUpCardRarity - GearUP");
            foreach (string item in gearUpRarityChecklist)
            {
                tempCardName = GearUpCards.ModInitials + "@" + item;
                tempCardInfo = GetCardInfo(tempCardName);
                if (tempCardInfo == null) continue;

                raritySnapshot[tempCardName] = RarityUtils.GetCardRarityModifier(tempCardInfo);
            }

            // Miscs.Log("[GearUp] SaveGearUpCardRarity - Vanilla Blocks");
            foreach (var item in cardListVanillaBlocks)
            {
                // Miscs.Log("> " + item);
                tempCardInfo = GetCardInfo(item);
                if (tempCardInfo == null) continue;

                raritySnapshot[item] = RarityUtils.GetCardRarityModifier(tempCardInfo);
            }

            // Miscs.Log("[GearUp] SaveGearUpCardRarity - Modded Blocks");
            foreach (var item in cardListModdedBlocks)
            {
                // Miscs.Log("> " + item);
                tempCardInfo = GetCardInfo(item);
                if (tempCardInfo == null) continue;

                raritySnapshot[item] = RarityUtils.GetCardRarityModifier(tempCardInfo);
            }
        }

        public static void RestoreGearUpCardRarity()
        {
            Miscs.Log("[GearUp] RestoreGearUpCardRarity()");
            CardInfo tempCardInfo;
            float tempMul;

            foreach (string item in raritySnapshot.Keys)
            {
                // Miscs.Log("> " + item);
                tempCardInfo = GetCardInfo(item);
                if (tempCardInfo == null) continue;

                // RarityUtils.SetCardRarityModifier
                // (
                //     tempCardInfo,
                //     raritySnapshot[item]
                // );

                tempMul = RarityUtils.GetCardRarityModifier(tempCardInfo);
                RarityUtils.AjustCardRarityModifier(tempCardInfo, mul: -1.0f * ((tempMul / raritySnapshot[item]) - 1.0f));

                // Miscs.Log("Restored! - " + raritySnapshot[item]);
                // Miscs.Log("Rarity set to: " + RarityUtils.GetCardRarityModifier(tempCardInfo));
            }
        }

        public static void BatchAdjustCardRarity(List<string> cardList, float add = 0.0f, float mul = 0.0f)
        {
            CardInfo tempCardInfo;

            foreach (string item in cardList)
            {
                // Miscs.Log($"> {item}");
                tempCardInfo = GetCardInfo(item);
                if (tempCardInfo == null) continue;

                RarityDelta.AdjustRarityModifier(tempCardInfo, add, mul);
                // RarityUtils.AjustCardRarityModifier(tempCardInfo, add, mul);
                // Miscs.Log("Got it");
            }
        }

        public static void ModifyPerPlayerCardRarity(int pickerID)
        {
            // Miscs.Log($"[GearUp] ModifyPerPlayerCardRarity({playerID})");
            Player targerPlayer = null;
            if (pickerID <= -1) return;

            foreach (Player item in PlayerManager.instance.players)
            {
                if (item.playerID == pickerID)
                {
                    targerPlayer = item;
                    break;
                }
            }

            if (targerPlayer == null) return;
            CharacterStatModifiers targetStats = targerPlayer.gameObject.GetComponent<CharacterStatModifiers>();
            CharacterStatModifiersGearData gearData = targetStats.GetGearData();

            // !! // spells and glyphs : Glyphs to boost spell, magick and specialized cards rate
            float tempModifier = 0.0f;

            tempModifier += gearData.glyphDivination * 2.00f;
            tempModifier += gearData.glyphGeometric * 0.50f;
            tempModifier += gearData.glyphInfluence * 2.50f;
            tempModifier += gearData.glyphPotency * 0.50f;
            tempModifier += gearData.glyphMagickFragment * 0.75f;
            tempModifier += gearData.glyphTime * 0.50f;
            tempModifier += gearData.glyphReplication * 0.50f;

            // Miscs.Log(">.<");
            // Miscs.Log("> Glyph base modifier: " + tempModifier);
            if (gearData.addOnList.Contains(GearUpConstants.AddOnType.cadModuleGlyph))
            {
                // tempModifier *= 1.25f;
                BatchAdjustCardRarity(cardListGlyph, mul: 2.00f);
            }
            else
            {
                //RarityUtils.AjustCardRarityModifier
                RarityDelta.AdjustRarityModifier
                (
                    GetCardInfo(GearUpCards.ModInitials, "Glyph CAD Module"),
                    mul: tempModifier * 1.50f - 0.75f
                );
            }
            BatchAdjustCardRarity(cardListSpells, mul: tempModifier);

            RarityDelta.AdjustRarityModifier
            (
                GetCardInfo(GearUpCards.ModInitials, "Pure Canvas"),
                mul: tempModifier * 0.5f - 0.5f
            );

            // !! // spells and glyphs : Spells/Magicks to boost glyph card rate
            tempModifier = gearData.orbLifeforceDualityStack * 0.50f;
            tempModifier += gearData.orbLifeforceBlastStack * 0.50f;
            tempModifier += gearData.orbObliterationStack * 0.50f;
            tempModifier += gearData.orbRollingBulwarkStack * 0.50f;
            tempModifier += gearData.arcaneSunStack * 0.50f;
            tempModifier += gearData.mysticMissileStack * 0.50f;
            if (gearData.uniqueMagick != GearUpConstants.ModType.disabled && gearData.uniqueMagick != GearUpConstants.ModType.none)
            {
                tempModifier += 2.00f;
            }
            BatchAdjustCardRarity(cardListGlyph, mul: tempModifier);

            RarityDelta.AdjustRarityModifier
            (
                GetCardInfo(GearUpCards.ModInitials, "Pure Canvas"),
                mul: tempModifier * 0.5f
            );

            // !! // [Empower] + [Shield Battery] boosting>> block abilities find chance
            tempModifier = (float)(gearData.shieldBatteryStack) * 0.25f;
            tempModifier += (float)(GetPlayerCardsWithName(targerPlayer, "Empower").Count) * 0.25f;
            tempModifier += gearData.glyphMagickFragment * 0.50f;

            // Miscs.Log(">.<");
            // Miscs.Log("> Block base modifier: " + tempModifier);

            //RarityUtils.AjustCardRarityModifier
            RarityDelta.AdjustRarityModifier
            (
                GetCardInfo(GearUpCards.ModInitials, "Shield Battery"),
                mul: tempModifier * 2.0f
            );
            //RarityUtils.AjustCardRarityModifier
            RarityDelta.AdjustRarityModifier
            (
                GetCardInfo("Empower", true),
                mul: tempModifier * 2.0f
            );

            if (gearData.addOnList.Contains(GearUpConstants.AddOnType.charmGuardian))
            {
                BatchAdjustCardRarity(cardListVanillaBlocks, mul: tempModifier * 2.0f);
                BatchAdjustCardRarity(cardListModdedBlocks, mul: tempModifier);
            }

            // !! // blocking ability boosting >> [Empower] + [Shield Battery] find chance
            List<PlayerCardData> tempList = GetPlayerCardsWithStringList(targerPlayer, cardListVanillaBlocks);
            tempModifier = (float)(tempList.Count) * 0.20f;

            tempList = GetPlayerCardsWithStringList(targerPlayer, cardListModdedBlocks);
            tempModifier += (float)(tempList.Count) * 0.10f;

            //RarityUtils.AjustCardRarityModifier
            RarityDelta.AdjustRarityModifier
            (
                GetCardInfo(GearUpCards.ModInitials, "Shield Battery"),
                mul: tempModifier
            );
            //RarityUtils.AjustCardRarityModifier
            RarityDelta.AdjustRarityModifier
            (
                GetCardInfo("Empower", true),
                mul: tempModifier
            );

            // blocking ability boosing>> more block cards
            if (gearData.addOnList.Contains(GearUpConstants.AddOnType.charmGuardian))
            {
                BatchAdjustCardRarity(cardListVanillaBlocks, mul: (tempModifier * 2.0f) + 3.0f);
                BatchAdjustCardRarity(cardListModdedBlocks, mul: tempModifier + 1.5f);
            }

            // [Medic Checkup!] weight based on player's MAX HP
            tempModifier = 0.0f;

            Player pickerPlayer = PlayerManager.instance.players[pickerID];
            float maxHP = pickerPlayer.data.maxHealth;

            if (maxHP < 100.0f)
            {
                tempModifier += 2.0f * (100.0f - maxHP) / 100.0f;
            }
            else if (maxHP > 500.0f)
            {
                tempModifier -= (maxHP - 500.0f) / 500.0f;
            }

            tempModifier = Mathf.Clamp(tempModifier, -0.999f, 3.0f);
            Miscs.Log("[GearUp] MEDIC!!! weight mul delta : " + tempModifier);

            RarityDelta.AdjustRarityModifier
            (
                GetCardInfo(GearUpCards.ModInitials, "Medic!!!"),
                mul: tempModifier
            );

            float medicCheckupWeight = RarityUtils.GetCardRarityModifier(GetCardInfo(GearUpCards.ModInitials, "Medic!!!"));
            Miscs.Log("[GearUp] MEDIC!!! current weight : " + medicCheckupWeight);

            if (medicCheckupWeight < 0)
            {
                RarityDelta.AdjustRarityModifier
                (
                    GetCardInfo(GearUpCards.ModInitials, "Medic!!!"),
                    add: 0.0001f - medicCheckupWeight
                );
                Miscs.LogWarn("[GearUp] MEDIC!!! weight gone negative!");
            }
        }

        public static string CardNameSanitize(string name, bool removeWhitespaces = false)
        {
            string result = name.ToLower();
            if (removeWhitespaces)
            {
                result = result.Replace(" ", "");
            }

            return result;
        }

        public static void MakeExclusive(string cardA, string cardB)
        {
            CardInfo cardInfoA, cardInfoB;
            cardInfoA = GetCardInfo(cardA);
            cardInfoB = GetCardInfo(cardB);

            if (cardInfoA != null && cardInfoB != null)
            {
                CustomCardCategories.instance.MakeCardsExclusive(cardInfoA, cardInfoB);

                Miscs.LogInfo($"[GearUp] MakeExclusive: card [{cardA}] and card [{cardB}] made exclusive");
            }
            else
            {
                if (cardInfoA == null)
                {
                    Miscs.Log($"[GearUp] MakeExclusive: card [{cardA}] not found");
                }
                if (cardInfoB == null)
                {
                    Miscs.Log($"[GearUp] MakeExclusive: card [{cardB}] not found");
                }
            }
        }

        // extra text (bottom-right) of the card, credit to Pykess
        public class DestroyOnUnparent : MonoBehaviour
        {
            void LateUpdate()
            {
                if (this.gameObject.transform.parent == null) { Destroy(this.gameObject); }
            }
        }

        internal class ExtraName : MonoBehaviour
        {
            private static GameObject _extraTextObj = null;
            internal static GameObject extraTextObj
            {
                get
                {
                    if (_extraTextObj != null) { return _extraTextObj; }

                    _extraTextObj = new GameObject("ExtraCardText", typeof(TextMeshProUGUI), typeof(DestroyOnUnparent));
                    DontDestroyOnLoad(_extraTextObj);
                    return _extraTextObj;


                }
                private set { }
            }

            public string text = "";

            public void Start()
            {
                // add extra text to bottom right
                // create blank object for text, and attach it to the canvas
                // find bottom right edge object
                RectTransform[] allChildrenRecursive = this.gameObject.GetComponentsInChildren<RectTransform>();
                GameObject BottomLeftCorner = allChildrenRecursive.Where(obj => obj.gameObject.name == "EdgePart (1)").FirstOrDefault().gameObject;
                GameObject modNameObj = UnityEngine.GameObject.Instantiate(extraTextObj, BottomLeftCorner.transform.position, BottomLeftCorner.transform.rotation, BottomLeftCorner.transform);
                TextMeshProUGUI modText = modNameObj.gameObject.GetComponent<TextMeshProUGUI>();
                modText.text = text;
                modText.enableWordWrapping = false;
                modNameObj.transform.Rotate(0f, 0f, 135f);
                modNameObj.transform.localScale = new Vector3(1f, 1f, 1f);
                modNameObj.transform.localPosition = new Vector3(-50f, -50f, 0f);
                modText.alignment = TextAlignmentOptions.Bottom;
                modText.alpha = 0.1f;
                modText.fontSize = 50;
            }
        }
    }
}
