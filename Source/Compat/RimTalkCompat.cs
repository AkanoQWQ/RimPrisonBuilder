using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimPrison.Compat
{
    [StaticConstructorOnStartup]
    public static class RimTalkCompat
    {
        private const string ModId = "rimprison";

        static RimTalkCompat()
        {
            LongEventHandler.ExecuteWhenFinished(Initialize);
        }

        private static void Initialize()
        {
            try
            {
                var apiType = AccessTools.TypeByName("RimTalk.API.RimTalkPromptAPI");
                if (apiType == null) return; // RimTalk not installed

                var method = apiType.GetMethod("RegisterPawnVariable",
                    BindingFlags.Public | BindingFlags.Static);
                if (method == null) return;

                // {{pawn.balance}} — current coupon balance
                method.Invoke(null, new object[]
                {
                    ModId,                           // modId
                    "balance",                       // variableName
                    (Func<Pawn, string>)BalanceProvider, // provider
                    "Rimprison coupon balance",                // description (for template authors)
                    100                              // priority
                });

                Log.Message("[RimPrison] RimTalk integration enabled — {{pawn.balance}}");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimPrison] RimTalk integration failed: {ex.Message}");
            }
        }

        private static string BalanceProvider(Pawn pawn)
        {
            if (pawn == null) return "0";
            return pawn.TryGetComp<PrisonLabor.CompWorkTracker>()?.earnedCoupons.ToString() ?? "0";
        }
    }
}
