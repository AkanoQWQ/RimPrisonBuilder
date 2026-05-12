using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    public class GameComponent_ActivityLog : MapComponent
    {
        private const int MaxEntries = 500;

        public List<LogEntry> entries = new List<LogEntry>();

        public struct LogEntry
        {
            public int tick;
            public string pawnName;
            public string message;

            public string Format()
            {
                int days = tick / 60000;
                int hours = (tick % 60000) / 2500;
                return $"Day {days} {hours}:00 — {pawnName}: {message}";
            }
        }

        public GameComponent_ActivityLog(Map map) : base(map) { }

        public void Log(string pawnName, string message)
        {
            entries.Add(new LogEntry
            {
                tick = Find.TickManager.TicksGame,
                pawnName = pawnName,
                message = message
            });
            // [OPTIMIZE] AI said it's efficient
            // Maybe a real ring-array is better, no GC!
            if (entries.Count > MaxEntries)
                entries.RemoveRange(0, entries.Count - MaxEntries);
        }

        public void Log(Pawn pawn, string message)
        {
            Log(pawn?.LabelShortCap ?? "?", message);
        }

        // [TODO] Match by NAME here NOT GOOD
        public List<LogEntry> GetRecentPawnEntries(Pawn pawn, int count)
        {
            if (pawn == null) return new List<LogEntry>();
            var result = new List<LogEntry>();
            for (int i = entries.Count - 1; i >= 0 && result.Count < count; i--)
            {
                if (entries[i].pawnName == pawn.LabelShortCap)
                    result.Add(entries[i]);
            }
            return result;
        }

        // [UNREVIEWED] Haven't reviewed ExposeData here
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var ticks = new List<int>();
                var names = new List<string>();
                var msgs = new List<string>();
                foreach (var e in entries)
                {
                    ticks.Add(e.tick);
                    names.Add(e.pawnName);
                    msgs.Add(e.message);
                }
                Scribe_Collections.Look(ref ticks, "logTicks", LookMode.Value);
                Scribe_Collections.Look(ref names, "logNames", LookMode.Value);
                Scribe_Collections.Look(ref msgs, "logMsgs", LookMode.Value);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var ticks = new List<int>();
                var names = new List<string>();
                var msgs = new List<string>();
                Scribe_Collections.Look(ref ticks, "logTicks", LookMode.Value);
                Scribe_Collections.Look(ref names, "logNames", LookMode.Value);
                Scribe_Collections.Look(ref msgs, "logMsgs", LookMode.Value);
                // [TODO] If save data is corrupted (e.g. mismatched list lengths from a broken save),
                // ticks/names/msgs may be null here, causing NRE on .Count. Add null guards.
                entries.Clear();
                int n = System.Math.Min(System.Math.Min(ticks.Count, names.Count), msgs.Count);
                for (int i = 0; i < n; i++)
                {
                    entries.Add(new LogEntry
                    {
                        tick = ticks[i],
                        pawnName = names[i],
                        message = msgs[i]
                    });
                }
            }
        }
    }
}
