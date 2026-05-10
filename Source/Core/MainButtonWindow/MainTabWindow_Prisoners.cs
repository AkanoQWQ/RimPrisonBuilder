using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison
{
    public class MainTabWindow_Prisoners : MainTabWindow
    {
        private Dialog_PrisonerManagement dialog;

        public override Vector2 RequestedTabSize => Vector2.zero;

        public override void PostOpen()
        {
            base.PostOpen();
            if (dialog == null || !dialog.IsOpen)
            {
                dialog = new Dialog_PrisonerManagement();
                Find.WindowStack.Add(dialog);
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            // Dialog was closed intentionally (X button)
            // Close this invisible tab so the next button click opens fresh.
            if (dialog != null && !dialog.IsOpen)
            {
                dialog = null;
                Find.MainTabsRoot.EscapeCurrentTab();
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            if (dialog != null && dialog.IsOpen)
            {
                dialog.Close();
            }
            dialog = null;
        }
    }
}
