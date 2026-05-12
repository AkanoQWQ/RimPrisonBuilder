using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.PrisonLabor
{
    // [UNREVIEWED] Oh this is an UI! I have valid reason for UNREVIEWED !
    public class Gizmo_Balance : Gizmo
    {
        private readonly Pawn pawn;
        private readonly CompWorkTracker tracker;

        public Gizmo_Balance(Pawn pawn, CompWorkTracker tracker)
        {
            this.pawn = pawn;
            this.tracker = tracker;
            Order = -90f; // right after standard prisoner gizmos
        }

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            float width = GetWidth(maxWidth);
            Rect rect = new Rect(topLeft.x, topLeft.y, width, 75f);
            Widgets.DrawWindowBackground(rect);

            // Balance value
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            string balanceText = tracker.earnedCoupons.ToString();
            Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width, 30f), balanceText);
            Text.Font = GameFont.Small;

            // Currency label
            Text.Font = GameFont.Tiny;
            var oldColor = GUI.color;
            GUI.color = new Color(0.72f, 0.74f, 0.76f, 1f);
            Widgets.Label(new Rect(rect.x, rect.y + 34f, rect.width, 20f),
                RimPrisonMod.Settings.WorkCouponName);
            GUI.color = oldColor;

            // Debt warning
            if (tracker.earnedCoupons < 0)
            {
                GUI.color = new Color(0.9f, 0.3f, 0.3f, 1f);
                Widgets.Label(new Rect(rect.x, rect.y + 54f, rect.width, 18f),
                    "RimPrison.InDebt".Translate());
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}
