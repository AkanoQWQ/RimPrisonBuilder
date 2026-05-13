using RimWorld;
using Verse;

namespace RimPrison.PrisonArea
{
    public abstract class Designator_AreaPrison : Designator_Cells
    {
        private DesignateMode mode;

        public override bool DragDrawMeasurements => true;
        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

        public Designator_AreaPrison(DesignateMode mode)
        {
            this.mode = mode;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            hotKey = KeyBindingDefOf.Misc7;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(base.Map))
                return false;

            var area = GetOrCreateArea();
            bool isInArea = area[c];
            if (mode == DesignateMode.Add)
                return !isInArea;
            return isInArea;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            var area = GetOrCreateArea();
            if (mode == DesignateMode.Add)
                area[c] = true;
            else
                area[c] = false;
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            GetOrCreateArea().MarkForDraw();
        }

        private Area_Prison GetOrCreateArea()
        {
            var area = base.Map.areaManager.Get<Area_Prison>();
            if (area == null)
                area = Area_Prison.CreateNew(base.Map);
            return area;
        }
    }
}
