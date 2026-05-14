using RimWorld;
using Verse;

namespace RimPrison.DefOfs
{
    [DefOf]
    public static class RP_DefOf
    {
        public static PrisonerInteractionModeDef RimPrison_AllowLabor;

        static RP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RP_DefOf));
        }
    }

    [DefOf]
    public static class RP_JobDefOf
    {
        public static JobDef RimPrison_TakeToCouponShop;
        public static JobDef RimPrison_BuyFromCouponShop;

        static RP_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RP_JobDefOf));
        }
    }

    [DefOf]
    public static class RP_HediffDefOf
    {
        public static HediffDef RPR_Despair;
        public static HediffDef RPR_RegimeHarsh;
        public static HediffDef RPR_RegimeDeterrence;
        public static HediffDef RPR_RegimeEquality;

        static RP_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RP_HediffDefOf));
        }
    }

    [DefOf]
    public static class RP_LetterDefOf
    {
        public static LetterDef RPR_RansomLetter;

        static RP_LetterDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RP_LetterDefOf));
        }
    }

    [DefOf]
    public static class RP_ThoughtDefOf
    {
        public static ThoughtDef RPR_BoughtPremiumFood;
        public static ThoughtDef RPR_BoughtDrug;
        public static ThoughtDef RPR_AllowanceGood;
        public static ThoughtDef RPR_FeeExploitation;

        static RP_ThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RP_ThoughtDefOf));
        }
    }
}
