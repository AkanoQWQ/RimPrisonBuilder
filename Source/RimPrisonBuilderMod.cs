using UnityEngine;
using Verse;
using HarmonyLib;

namespace RimPrisonBuilder
{
    public class RimPrisonBuilderMod : Mod
    {
        public static RimPrisonBuilderSettings Settings;

        public RimPrisonBuilderMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimPrisonBuilderSettings>();
            var harmony = new Harmony("akano.rimprisonbuilder");
            harmony.PatchAll();
            Log.Message("Hello Rimworld from RimPrisonBuilder!");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimPrisonBuilder";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }
}
