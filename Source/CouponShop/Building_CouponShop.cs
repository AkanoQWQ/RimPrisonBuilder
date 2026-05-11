using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPrisonBuilder.CouponShop
{
    public class Building_CouponShop : Building, IStoreSettingsParent
    {
        // Component of coupon shop
        private CompCouponShop couponComp;
        public CompCouponShop CouponComp =>
            couponComp ?? (couponComp = GetComp<CompCouponShop>());

        public int PricePerItem => CouponComp?.pricePerItem ?? 1;
        public bool HasSpace => CouponComp?.HasSpace ?? false;
        public bool HasStock => CouponComp?.HasStock ?? false;

        public bool Accepts(Thing t) => CouponComp?.CanDeposit(t) ?? false;

        // Show storage in inspect UI
        public bool StorageTabVisible => true;

        public StorageSettings GetStoreSettings()
        {
            var comp = CouponComp;
            if (comp == null)
            {
                return null;
            }
            // CompCouponShop.Initialize() already creates storageSettings,
            // so this branch should never execute. Kept as a safety net.
            if (comp.storageSettings == null)
            {
                comp.storageSettings = new StorageSettings();
                comp.storageSettings.filter.SetAllowAll(null);
                comp.storageSettings.owner = this;
            }
            return comp.storageSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return null;
        }

        // No need for filter edition callback
        public void Notify_SettingsChanged() { }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            couponComp = GetComp<CompCouponShop>();
            if (couponComp?.storageSettings != null)
            {
                couponComp.storageSettings.owner = this;
            }
        }

        public override string GetInspectString()
        {
            string str = base.GetInspectString();
            var comp = CouponComp;
            if (comp != null)
            {
                if (!str.NullOrEmpty())
                {
                    str += "\n";
                }
                if (comp.stockCount > 0 && comp.storedItemDef != null)
                {
                    str += comp.storedItemDef.LabelCap + " ";
                }
                str += "RimPrisonBuilder.ShopStock".Translate(comp.stockCount, comp.Capacity);
                str += "\n";
                str += "RimPrisonBuilder.PricePerItem".Translate() + ": " + comp.pricePerItem;
            }
            return str;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
            yield return new Command_Action
            {
                defaultLabel = "RimPrisonBuilder.SetPrice".Translate(),
                defaultDesc = "RimPrisonBuilder.SetPriceDesc".Translate(),
                icon = TexCommand.DesirePower,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_SetPrice(this));
                }
            };
        }
    }
}
