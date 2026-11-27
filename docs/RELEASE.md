---
layer: release
status: living
---
# Unity.Rea.Overworld — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
A Unity 6 rebuild of the 2006 VB.NET / DirectX 9 *Overworld* prototype: a mouse-driven top-down adventure for a party of three, with a living clock, weather, a script language and ring-menu battles (BIBLE).

## 2. The milestone: Unity.Rea.Overworld faithful port {#milestone}
- **Milestone:** MS-82, "Unity.Rea.Overworld faithful playable port"
- **Published means:** Development and Release Windows players at `R:\MindAttic\Apps\Unity.Rea.Overworld\{Debug,Release}`
  (internal; not on a store).
- **The natural stopping point:** the port is already built; this milestone confirms it as a deployed, playable,
  bug-fixed Release player that plays as the original did, then stops. Owner rule: resurrect the original
  faithfully, fix bugs only, never change the gameplay.

## 3. Done when (acceptance checks)
1. build — `run.ps1 -Redeploy` builds both Windows players clean on main.
2. exists — `R:\MindAttic\Apps\Unity.Rea.Overworld\Release` holds a player that launches.
3. signoff — the owner plays the Release player end to end: walk the world, pick up and use items, run Farmer Brinn's quests, fight a battle, sleep through the night.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | U.OVW-T109 Faithful port | Confirmed bugs fixed, a recorded Release play-through | U.OVW-T110 Port bug fixes (990), U.OVW-T111 Release play-through (980) |
| 800 | U.OVW-T112 Windows release | Players deployed on R:, owner sign-off | U.OVW-T113 Players on R: (890), U.OVW-T114 Owner sign-off (880) |

## 5. Out of scope (not before this milestone)
- Any change or improvement to gameplay, controls feel or content: the owner rule is resurrect the original faithfully and fix bugs only.
- Online play beyond what is already wired (the WorldMirror client view).
- New quests, areas or battle systems the prototype never had.
- Mobile, console or store builds.

## 6. Current verified state
- Every build-order story is built and an end-to-end verification harness runs clean (7749120, U.OVW-T37; stories A20-A33 done, U.OVW-T98).
- Two bughunt passes landed (888e41b, ea9c5e8, U.OVW-T97).
- The shared run menu deploys to R: (472742a, MAT-T311); nothing is under R:\MindAttic\Apps yet.

## 7. Decision record
- 2026-11-27 — charter drafted from README/BIBLE/RECONSTRUCTION/tickets; Legion vote: Approve, plurality quorum reached, 7/9 voters answered, all Approve; no dissent (the rest had no provider key).
- Triage: 2 open -> done 0, duplicate 0, wont-do 0, kept 2 (U.OVW-T105 bughunt reworded to a bounded task); new tasks 2 (Release play-through, owner sign-off).