using Verse;
using HarmonyLib;

namespace RimPrison
{
    public class RimPrisonMod : Mod
    {
        public RimPrisonMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("akano.rimprison");
            harmony.PatchAll();
            Log.Message("Hello Rimworld from RimPrison!");
        }
    }
}
