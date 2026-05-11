using RimWorld;
using Verse;

namespace RimPrisonBuilder.DefOfs
{
    [DefOf]
    public static class RP_DefOf
    {
        public static PrisonerInteractionModeDef RimPrisonBuilder_AllowLabor;

        static RP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RP_DefOf));
        }
    }

    [DefOf]
    public static class RP_JobDefOf
    {
        public static JobDef RimPrisonBuilder_TakeToCouponShop;
        public static JobDef RimPrisonBuilder_BuyFromCouponShop;

        static RP_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RP_JobDefOf));
        }
    }
}
