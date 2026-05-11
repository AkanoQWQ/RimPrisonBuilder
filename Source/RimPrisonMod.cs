using UnityEngine;
using Verse;
using HarmonyLib;

namespace RimPrison
{
    public class RimPrisonMod : Mod
    {
        public static RimPrisonSettings Settings;

        public RimPrisonMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimPrisonSettings>();
            var harmony = new Harmony("akano.rimprison");
            harmony.PatchAll();
            Log.Message("Hello Rimworld from RimPrison!");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimPrison";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }
}
