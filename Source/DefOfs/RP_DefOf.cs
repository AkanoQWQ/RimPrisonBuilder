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
}
