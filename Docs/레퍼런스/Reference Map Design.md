# Map Design
---
- Notes Claude uses when building graybox maps for prototyping. Written in English to save tokens.
- Studied 2026-10-28: OneShot, OMORI, Undertale. Black Souls and Ib are not studied yet.
- Sources: OneShot region maps by Gavi Guy (images from the user), OMORI Map Guide (e-dal.github.io/omori-map-guide), Undertale Wiki interactive maps (Ruins, Snowdin), web articles on 2D action-adventure and horror map design.
- Observations are what the maps show. Principles are Claude's reading of them and are not decisions. The user designs the real maps; these exist only so the prototype "feels like an adventure".


# Screen size
---
- Measure scale by "character height / screen height", not by tile count.

| Game | Screen in tiles | Character height / screen |
|---|---|---|
| Ib | 20×15 (16px) | ~1/7.5 |
| OneShot | 20×15 (32px) | ~1/10 |
| OMORI | 20×15 (32px) | ~1/10 |
| Undertale | ~16×12 (20px, estimated) | ~1/8 |
| Depersonalization | unknown (~480×270 estimated) | ~1/6 |
| Gaze now | 20×15 (16px, PixelPerfectCamera ref 320×240) | ~1/15 |

- Gaze's camera was ~34×19 (girl 2–3× smaller than any reference) until 2026-10-29; it now matches the references. Room sizes below assume a 20×15 screen.


# OneShot
---
- **Region = one long outdoor path + small interiors hanging off it.**
	- The Barrens: Start Point → Outpost → Cliffs → Old Factory → Construction → Swamp → Docks. The path bends back toward the start, so the region is a big loop. Exit to the next region is at the Docks.
	- A second branch (Cliffs → Lookout Point → Mineshaft) runs as a chain of narrow horizontal rooms.
- **Outdoors are wide and sparse, interiors are small and dense.** Interiors are mostly one screen or less.
- **The Glen is hub-and-spoke.** The arrival point (Docks) branches to Village (north), Forest → Research Station (south), and Ruins (east). The Ruins interior is the only maze-like part.
- **The Refuge is vertical and symmetric.** Factory floors stack upward, each split left / center / right. The main street (Ground 1, Ground 2) is a long horizontal strip with doors along it.
- **Connections are typed.** The map legend separates: walked-through, cutscene, door, one-way. One-way paths are marked on purpose.


# OMORI (Headspace)
---
- **Central hub with arms.** Vast Forest is a cross: a central plaza with paths north, south, east, west. Regions (Pyrefly Forest, Otherworld, etc.) hang off the arms.
- **The hub wraps around.** Walking off the east edge comes back on the west edge (shown as a loop on the map). The hub feels endless without being large.
- **Pyrefly Forest is a cluster of screen-sized chunks**, connected room to room, branching to a building at one end and a chain of rooms at the other. Warps (dashed lines) link distant chunks.
- **Collectibles sit on side branches** (watermelons, items), not on the main line. Side branches pay the player for looking.


# Undertale
---
- **Regions are almost linear.** Ruins: start → up → a long east corridor → a cluster of puzzle rooms → up to Toriel's Home → a long vertical corridor to the exit. Snowdin is the same: a long horizontal chain ending at the town.
- **Corridors are one screen tall.** Most rooms are horizontal strips; the player mostly walks left to right.
- **Detours are short pockets** off the line (one room, then back). They hold puzzles, items, or jokes.
- **Long empty corridors are used deliberately** before important moments (the end of the Ruins before Toriel's fight).


# Common patterns
---
- **A spine.** Every region has one clear main path. Branches are short and return to it.
- **Rhythm of size.** Wide sparse spaces alternate with small dense rooms. The change of size is itself a beat.
- **Side branches pay off.** Something worth finding sits off the main line: an item, a character, a scene.
- **Loops back home.** The path often returns near where it started (OneShot Barrens, OMORI wrap-around), so the player feels the place has shape.
- **Typed connections.** Doors, one-way drops, and cutscene transitions are different tools. A one-way path means "you can't go back".
- **Small rooms, not big empty squares.** Horror and RPG Maker mapping advice agrees: cramped spaces feel trapped; large empty square maps feel dead.


# For the first graybox
---
- One region = start → (tension) → save point. Tension is not designed yet; leave marked slots for it.
- Use the spine + short branch + loop pattern. Size: a few screens long at a ~20×15 screen.
- Put at least one side pocket with something the girl reacts to (a "shiny stone" moment).
- Leave room for a chase: a corridor long enough to run, and a place to hide or a door to shut.
