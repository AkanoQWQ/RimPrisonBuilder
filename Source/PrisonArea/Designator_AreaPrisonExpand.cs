using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.PrisonArea
{
    public class Designator_AreaPrisonExpand : Designator_AreaPrison
    {
        public Designator_AreaPrisonExpand()
            : base(DesignateMode.Add)
        {
            defaultLabel = "RimPrison.ExpandPrisonArea".Translate();
            defaultDesc = "RimPrison.ExpandPrisonAreaDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/HomeAreaOn");
            soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
            soundSucceeded = SoundDefOf.Designate_ZoneAdd;
            hotKey = KeyBindingDefOf.Misc4;
        }
    }
}
