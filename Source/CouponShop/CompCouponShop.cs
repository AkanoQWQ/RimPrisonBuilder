using RimWorld;
using Verse;

namespace RimPrisonBuilder.CouponShop
{
    public class CompProperties_CouponShop : CompProperties
    {
        public int defaultPrice = 1;
        public int capacity = 5;

        public CompProperties_CouponShop()
        {
            compClass = typeof(CompCouponShop);
        }
    }

    public class CompCouponShop : ThingComp
    {
        public int pricePerItem = 1;
        public int stockCount;
        public ThingDef storedItemDef;
        public StorageSettings storageSettings;

        public CompProperties_CouponShop Props => (CompProperties_CouponShop)props;
        public int Capacity => Props.capacity;
        public bool HasSpace => stockCount < Capacity;
        public bool HasStock => stockCount > 0;
        public ThingFilter Filter => storageSettings?.filter;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            pricePerItem = Props.defaultPrice;
            storageSettings = new StorageSettings();
            storageSettings.filter.SetAllowAll(null);
        }

        public bool Allows(Thing t)
        {
            if (storageSettings == null)
            {
                return false;
            }
            return storageSettings.AllowedToAccept(t);
        }

        // Only same def allowed when shop already has stock
        public bool CanDeposit(Thing t)
        {
            if (!Allows(t))
            {
                return false;
            }
            if (stockCount > 0 && storedItemDef != null && t.def != storedItemDef)
            {
                return false;
            }
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pricePerItem, "pricePerItem", 1);
            Scribe_Values.Look(ref stockCount, "stockCount", 0);
            Scribe_Defs.Look(ref storedItemDef, "storedItemDef");
            Scribe_Deep.Look(ref storageSettings, "storageSettings");
        }
    }
}
