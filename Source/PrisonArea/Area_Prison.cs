using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.PrisonArea
{
    public class Area_Prison : Area
    {
        public override string Label => "RimPrison.PrisonArea".Translate();
        public override Color Color => new Color(0.6f, 0.1f, 0.1f, 0.7f);
        public override int ListPriority => 900;
        public override bool Mutable => false;

        private string labelCache;
        public override string RenamableLabel
        {
            get => labelCache ?? Label;
            set => labelCache = value;
        }
        public override string BaseLabel => Label;

        public Area_Prison() { }
        public Area_Prison(AreaManager areaManager) : base(areaManager) { }

        public override string GetUniqueLoadID() => $"Area_Prison_{ID}";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref labelCache, "labelCache");
        }

        public override bool AssignableAsAllowed() => true;

        public static Area_Prison CreateNew(Map map)
        {
            var area = new Area_Prison(map.areaManager);
            map.areaManager.AllAreas.Add(area);
            return area;
        }
    }
}
