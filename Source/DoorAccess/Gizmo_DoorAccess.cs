using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.DoorAccess
{
    // [UNREVIEWED] Rewrite soon
    public class Gizmo_DoorAccess : Gizmo
    {
        private readonly Comp_DoorAccess comp;

        public Gizmo_DoorAccess(Comp_DoorAccess comp)
        {
            this.comp = comp;
            Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 200f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 50f);
            Widgets.DrawWindowBackground(rect);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, rect.y + 2f, rect.width, 20f),
                "RimPrison.DoorAccess".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            Rect btnRect = new Rect(rect.x + 8f, rect.y + 24f, rect.width - 16f, 22f);
            if (Widgets.ButtonText(btnRect, comp.allowPrisoners
                ? "RimPrison.DoorAccessAllowed".Translate()
                : "RimPrison.DoorAccessBlocked".Translate()))
            {
                comp.allowPrisoners = !comp.allowPrisoners;
                (comp.parent as Building_Door)?.Map?.reachability?.ClearCache();
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }
}
