using System;
using RimPrisonBuilder.PrisonLabor;
using UnityEngine;
using Verse;

namespace RimPrisonBuilder.UI
{
    // [UNREVIEWED] Haven't reviewed carefully
    public class Dialog_ManagePrisonerGroups : Window
    {
        private PrisonerGroupManager manager;

        public Dialog_ManagePrisonerGroups(PrisonerGroupManager manager)
        {
            this.manager = manager;
            doCloseX = true;
            doCloseButton = false;
            forcePause = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            optionalTitle = "RimPrisonBuilder.ManagePrisonerGroups".Translate();
        }

        public override Vector2 InitialSize => new Vector2(500f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.ColumnWidth = inRect.width;
            listing.Begin(inRect);

            for (int i = 0; i < manager.groups.Count; i++)
            {
                Rect rowRect = listing.GetRect(30f);
                DrawGroupRow(rowRect, manager.groups[i]);
                listing.Gap(4f);
            }

            if (listing.ButtonText("RimPrisonBuilder.NewGroup".Translate()))
            {
                string name = "RimPrisonBuilder.DefaultGroupName".Translate() + " " + (manager.groups.Count + 1);
                manager.groups.Add(new PrisonerGroup(name));
            }

            listing.End();
        }

        private void DrawGroupRow(Rect rect, PrisonerGroup group)
        {
            Widgets.DrawHighlightIfMouseover(rect);

            float buttonWidth = 24f;
            float gap = 4f;
            float nameWidth = rect.width - (buttonWidth + gap) * 2;

            // Group name
            Rect nameRect = new Rect(rect.x, rect.y, nameWidth, rect.height);
            Widgets.Label(nameRect, group.name);

            // Rename button
            Rect renameRect = new Rect(rect.x + nameWidth + gap, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonImage(renameRect, TexButton.Rename))
            {
                Find.WindowStack.Add(new Dialog_RenamePrisonerGroup(group));
            }

            // Delete button
            Rect deleteRect = new Rect(rect.x + nameWidth + buttonWidth + gap * 2, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RimPrisonBuilder.DeleteGroupConfirm".Translate(group.name),
                    delegate
                    {
                        // Move all pawns in this group out before removing
                        for (int i = group.pawnThingIds.Count - 1; i >= 0; i--)
                        {
                            group.pawnThingIds.RemoveAt(i);
                        }
                        manager.groups.Remove(group);
                    }));
            }
        }
    }

    public class Dialog_RenamePrisonerGroup : Dialog_Rename<PrisonerGroup>
    {
        public Dialog_RenamePrisonerGroup(PrisonerGroup group) : base(group) { }
    }
}
