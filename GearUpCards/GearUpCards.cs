﻿using BepInEx;
using UnboundLib;
using UnboundLib.Cards;
using GearUpCards.Cards;
using HarmonyLib;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;

namespace GearUpCards
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]

    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]

    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]

    public class GearUpCards : BaseUnityPlugin
    {
        private const string ModId = "com.pudassassin.rounds.GearUpCards";
        private const string ModName = "GearUpCards";
        public const string Version = "0.0.3";

        public const string ModInitials = "Gear Up";

        public static GearUpCards Instance { get; private set; }

        void Awake()
        {
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
        void Start()
        {
            Instance = this;

            CustomCard.BuildCard<HollowLifeCard>();
        }
    }
}