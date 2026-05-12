using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.PrisonArea
{
    public class Designator_AreaPrisonClear : Designator_AreaPrison
    {
        public Designator_AreaPrisonClear()
            : base(DesignateMode.Remove)
        {
            defaultLabel = "RimPrison.ClearPrisonArea".Translate();
            defaultDesc = "RimPrison.ClearPrisonAreaDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/HomeAreaOff");
            soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
            soundSucceeded = SoundDefOf.Designate_ZoneAdd;
            hotKey = KeyBindingDefOf.Misc5;
        }
    }
}
