using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

using CardChoiceSpawnUniqueCardPatch.CustomCategories;

using GearUpCards.Utils;

namespace GearUpCards.Extensions
{
    public static class GearUpConstants
    {
        public enum ModType
        {
            disabled = -2,
            none = -1,

            crystal = 0,

            crystalNormal = 10,
            crystalTiberium,
            crystalPyro,
            crystalCryo,
            crystalMimic,

            sizeNormalize = 20,
            sizeShrinker,

            magickAntiBullet = 30,
            magickPortal,
            magickTimeDilution,

            gunSpreadArc = 40,
            gunSpreadLine,
            gunSpreadParallel,
            gunSpreadFlak
        }

        public enum AddOnType
        {
            gunBulletsDotRar = 0,

            cadModuleGlyph = 50,

            charmGuardian = 100
        }
    }

    // Add fields to CharacterStatModifiers
    // Using PCE's extension as coding reference

    public class CharacterStatModifiersGearData
    {
        // card stacks
        public int tacticalScannerStack;
        public int shieldBatteryStack;
        public int desolationStack;

        public int medicalPartStack;
        public int medicCheckupStack;

        public int hyperRegenerationStack;
        public int hollowLifeStack;

        public int chompyBulletStack;
        public int tiberiumBulletStack;

        // glyphs stacks
        public int glyphMagickFragment;

        public int glyphDivination;
        public int glyphInfluence;
        public int glyphGeometric;
        public int glyphPotency;
        public int glyphPiercing;
        public int glyphTime;
        public int glyphReplication;
        public int glyphProtection;

        // spell stacks
        public int orbObliterationStack;
        public int orbRollingBulwarkStack;
        public int orbLifeforceDualityStack;
        public int orbLifeforceBlastStack;

        public int arcaneSunStack;
        public int mysticMissileStack;
        public int arcaneConversionStack;

        //
        public float hpPercentageRegen;

        public GearUpConstants.ModType gunMod;
        public GearUpConstants.ModType gunSpreadMod;
        public GearUpConstants.ModType blockMod;
        public GearUpConstants.ModType sizeMod;

        public GearUpConstants.ModType uniqueMagick;

        public List<GearUpConstants.AddOnType> addOnList;

        public float t_uniqueMagickCooldown;

        //

        public CharacterStatModifiersGearData()
        {
            tacticalScannerStack = 0;
            shieldBatteryStack = 0;
            desolationStack = 0;

            medicalPartStack = 0;
            medicCheckupStack = 0;

            hyperRegenerationStack = 0;
            hollowLifeStack = 0;

            chompyBulletStack = 0;
            tiberiumBulletStack = 0;

            glyphMagickFragment = 0;
            glyphDivination = 0;
            glyphInfluence = 0;
            glyphGeometric = 0;
            glyphPotency = 0;
            glyphPiercing = 0;
            glyphTime = 0;
            glyphReplication = 0;
            glyphProtection = 0;

            orbObliterationStack = 0;
            orbRollingBulwarkStack = 0;
            orbLifeforceDualityStack = 0;
            orbLifeforceBlastStack = 0;

            arcaneSunStack = 0;
            mysticMissileStack = 0;
            arcaneConversionStack = 0;

            // 1.0f being 100%/s!!
            hpPercentageRegen = 0.0f;

            gunMod = GearUpConstants.ModType.none;
            gunSpreadMod = GearUpConstants.ModType.none;
            blockMod = GearUpConstants.ModType.none;
            sizeMod = GearUpConstants.ModType.none;

            uniqueMagick = GearUpConstants.ModType.none;

            addOnList = new List<GearUpConstants.AddOnType>();
        }
    }

    public static class CharacterStatModifiersGearDataExtensions
    {
        public static readonly ConditionalWeakTable<CharacterStatModifiers, CharacterStatModifiersGearData> data =
            new ConditionalWeakTable<CharacterStatModifiers, CharacterStatModifiersGearData>();

        public static CharacterStatModifiersGearData GetGearData(this CharacterStatModifiers characterStat)
        {
            return data.GetOrCreateValue(characterStat);
        }

        public static void AddData(this CharacterStatModifiers characterStat, CharacterStatModifiersGearData value)
        {
            try
            {
                data.Add(characterStat, value);
            }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(CharacterStatModifiers), "ResetStats")]
    class CharacterStatModifiersPatchResetStats
    {
        private static void Prefix(CharacterStatModifiers __instance)
        {
            __instance.GetGearData().tacticalScannerStack = 0;
            __instance.GetGearData().shieldBatteryStack = 0;
            __instance.GetGearData().desolationStack = 0;

            __instance.GetGearData().medicalPartStack = 0;
            __instance.GetGearData().medicCheckupStack = 0;

            __instance.GetGearData().hyperRegenerationStack = 0;
            __instance.GetGearData().hollowLifeStack = 0;

            __instance.GetGearData().chompyBulletStack = 0;
            __instance.GetGearData().tiberiumBulletStack = 0;

            __instance.GetGearData().glyphMagickFragment = 0;

            __instance.GetGearData().glyphDivination = 0;
            __instance.GetGearData().glyphInfluence = 0;
            __instance.GetGearData().glyphGeometric = 0;
            __instance.GetGearData().glyphPotency = 0;
            __instance.GetGearData().glyphPiercing = 0;
            __instance.GetGearData().glyphTime = 0;
            __instance.GetGearData().glyphReplication = 0;
            __instance.GetGearData().glyphProtection = 0;

            __instance.GetGearData().orbObliterationStack = 0;
            __instance.GetGearData().orbRollingBulwarkStack = 0;
            __instance.GetGearData().orbLifeforceDualityStack = 0;
            __instance.GetGearData().orbLifeforceBlastStack = 0;

            __instance.GetGearData().arcaneSunStack = 0;
            __instance.GetGearData().mysticMissileStack = 0;
            __instance.GetGearData().arcaneConversionStack = 0;

            __instance.GetGearData().hpPercentageRegen = 0.0f;

            __instance.GetGearData().gunMod = GearUpConstants.ModType.none;
            __instance.GetGearData().gunSpreadMod = GearUpConstants.ModType.none;
            __instance.GetGearData().blockMod = GearUpConstants.ModType.none;
            __instance.GetGearData().sizeMod = GearUpConstants.ModType.none;

            __instance.GetGearData().uniqueMagick = GearUpConstants.ModType.none;

            __instance.GetGearData().addOnList = new List<GearUpConstants.AddOnType> ();
        }
    }
}
