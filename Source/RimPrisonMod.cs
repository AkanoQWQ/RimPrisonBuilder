using UnityEngine;
using Verse;

namespace RimPrison
{
    public class RimPrisonMod : Mod
    {
        public static RimPrisonSettings Settings;

        public RimPrisonMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimPrisonSettings>();
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

    [StaticConstructorOnStartup]
    public static class RimPrisonStartup
    {
        static RimPrisonStartup()
        {
            var harmony = new HarmonyLib.Harmony("akano.rimprison");
            harmony.PatchAll();
            Verse.Log.Message("Hello Rimworld from RimPrison!");
        }
    }
}
