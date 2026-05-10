# TODO

## Long-term plan

1. **Rename the mod!** — We have a name collision! I want to pay homage to "Prison Architect" but can't use the exact name — need a clever variation that nods to it while staying legally distinct.

2. **Fix the 10-job loop bug** — The "colonist fails to carry, job rebuilds, loops 10 times" error still happens occasionally. Needs root-cause investigation and proper fix.

3. **Prisoner Rehabilitation System** — Inspired by Prison Architect. Prisoners can be reformed through labor, confinement, and recreation. Their original recruitment resistance is overridden: ALL of these needs (labor, security, recreation) must be satisfied before recruitment becomes possible. Prison isn't just punishment — it's about becoming a better person.

4. **Prisoner Records & Visitor System** — Also inspired by Prison Architect. Each prisoner gets a personal dossier (arrest date, labor hours, coupons earned, purchase history, misconduct count). Family/friends can visit, boosting prisoner mood and rehabilitation progress.

5. **Anti-Escape / Suppression System** — Content prisoners shouldn't try to escape. When a prisoner's needs are met (food, rest, recreation, labor satisfaction), escape attempt probability drops dramatically or becomes zero. The best prison security is a well-run prison.

6. **Parole & Hearing System** — Replace vanilla's simplistic recruitment mechanic. When a prisoner's reform value reaches a threshold, they can choose to leave as a visitor OR join the colony. The decision weighs their relationships with colonists and the quality of treatment they received — prisoners who were treated well are far more likely to stay. Freedom earned through genuine reform, not bought with silver. This is our answer to the competitor's "pay money = freedom" mechanic.

7. **Prisoner Reform Programs** — Give players positive reinforcement for keeping prisoners alive and invested in. Structured reform courses (humanities, trades, therapy) let prisoners:
   - **Gain skill XP** — labor becomes learning, prisoners get better at what they do.
   - **Shed negative traits** — e.g., humanities program can remove Psychopath, therapy can ease Volatile.
   - **Earn positive traits** — e.g., humanities program grants Kind, trade program grants Industrious.
   - **Work efficiency buff** — reformed prisoners work faster and better, giving players a tangible reason to run reform programs instead of harvesting organs. The carrot, not the stick.

## Short-term plan

1. Add prisoner outfit/drug management UI, then enable `OptimizeApparel` and `SatisfyChemicalNeed` in the prisoner ThinkTree.

2. Add group configuration of prisoner.(Jobs, time management...)