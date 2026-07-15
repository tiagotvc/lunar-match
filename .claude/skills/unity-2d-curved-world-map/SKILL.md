---
name: unity-2d-curved-world-map
description: Build a Candy-Crush-style pseudo-3D curved level-select path/world map for this Unity 2D project. Use when working on the map/path screen, level node layout, map camera, or map background/terrain art.
---

# Curved pseudo-3D world map (SweetSugar level-select screen)

Goal: a Candy-Crush-style tight winding/switchback road with rich scenery, matching the reference the user shared (phone-mockup, "Lemon Lake" section — tight serpentine spiral, continuous textured road, gold-bordered nodes, trees/shops/banners beside the road, at least one specially-styled bonus node), **without leaving 2D/orthographic rendering**.

## Status: this already exists in `gameStatic.unity`, it was just switched off

Turns out the work described below as a "plan" was already built by whoever made this asset — it was disabled/hidden by three separate issues, all now fixed (2026-07-15) and pushed:

1. `LevelsMap > Map details` (contains a full curved-path decoration set: `Path` with `path-1.png`..`path-5.png` — pre-baked serpentine candy-rope-striped curve art, one of Unity's actual official style references, not a straight line — plus `Separations`, `Grass`, `Flower`, `Trees anim`, `Buildings` (7 houses), 9 `map-item-N` decorations, and `map_banner_1` zone banners) was **`m_IsActive: 0`**. Re-enabled.
2. `LevelsMap > CanvasMap > SafeArea > Start` (the `StaticMapPlay.cs` "Level N, tap to Play" panel) was hardcoded **`m_IsActive: 1` with no dismiss button**, permanently covering the map on every scene load — confirmed even the game's own "back to map" flow just reloads the scene and re-shows this same panel. Disabled so the map is reachable at all. **This is a stopgap, not real UX** — the production fix is presumably a real dismiss/back button on this panel, not leaving it off forever; revisit before shipping.
3. `LevelsMap > CanvasMap > SafeArea > Background` was a full-screen placeholder image (bakery street art carrying the original asset's "Candy Smith" watermark — the actual name of this Unity Asset Store template, "SweetSugar" is this project's internal rename) sitting on top of the real map. Disabled. The real per-section backgrounds are `map_background_01`..`05` (also found disabled, sprites like `Map_1.png`, sorting order `-1` so they sit behind everything) — those are now enabled instead.

Visually confirmed working in the Simulator after these three fixes: curved candy-striped path, round nodes, trees, a shop building, a "Sweet Tooth City" zone banner — genuinely close to the Candy Crush reference already.

**Still open / not yet verified:**
- The very first screenshots shown at the start of this investigation depicted a plain near-straight path of small pink dots over a flat PNG — that visual's source was never located (not `Map details`, not `Levels`, not the second empty `Path` object). It may live in `game.unity` (the other, non-static map scene) rather than `gameStatic.unity`, or may not be real load-bearing code. Check `game.unity` before assuming `gameStatic.unity` is the only scene that matters.
- Whether the actual clickable `MapLevel` nodes (under `LevelsMap > Levels`, `Level01`..`Level100` prefab instances, y-span roughly -5.5 to 85) visually line up node-for-node with the decorative curve's node positions in `Map details` — both occupy the same coordinate neighborhood (confirmed) but pixel-perfect alignment wasn't confirmed.
- `WaypointsMover.cs`'s commented-out curved-movement code (see below) chases `SplineCurve`'s procedural Catmull-Rom curve through waypoint Transforms — that's a *different* curve than the hand-painted `path-N.png` art. Re-enabling it won't automatically make the walking icon hug the painted rope unless the waypoints were authored to trace it.

## The real technique (this genre doesn't use a literal 3D camera)

Games like Candy Crush Saga, Coin Master, Angry Birds 2, etc. do **not** render an actual tilted 3D globe per node. The illusion is built from three layered tricks, combined:

1. **A textured road following a tight spline**, not a thin line connecting dots — the road/path *is* a piece of ground art that bends, complete with its own texture (cobblestone, sand, candy-cane stripes) tiling correctly around corners. This project already has the exact Unity tool for this and isn't using it yet — see below.
2. **Forced-perspective terrain art**: the background is hand-painted/authored to already imply hills, elevation and depth (large foreground elements, smaller background elements, hills the path visually climbs over). The path curve is drawn to follow the *painted* terrain shape — the "3D" is baked into the art, not computed by the engine.
3. **Parallax**: background layers (sky/horizon, distant hills, midground, foreground props) scroll at different speeds as the camera pans, which is what actually sells "depth" to the eye far more than any camera trick.

An optional fourth layer of polish is a **curved-world vertex shader** (offsets a ground mesh's Y based on distance-from-camera², the classic Subway Surfers/Temple Run "horizon curves away" effect). It's a nice-to-have for extra polish, not how the core snake-path illusion is made — see caveat below before reaching for it.

## Key finding: the right tool is already installed, unused

`Packages/manifest.json` already depends on **`com.unity.2d.spriteshape` (14.0.1)** — the Unity package built exactly for this: author a 2D spline, assign a "Spline Profile" with an edge sprite (e.g. the cobblestone road texture), and it auto-tiles/wraps that texture around every curve and corner, including tight switchbacks, with correct sorting-layer 2D rendering. **No `SpriteShapeController` or `SpriteShapeProfile` asset exists anywhere in the project yet** (confirmed via search) — this package was pulled in as a dependency but the map was built without it, using flat PNGs instead. This should be the primary tool for step 1 below, not a hand-rolled `LineRenderer`/mesh-strip.

**Do not switch the map camera to Perspective projection.** The project's render pipeline is **URP with the 2D Renderer** (`Assets/Settings/Renderer2D.asset`, confirmed via `ProjectSettings/GraphicsSettings.asset`), which is built around orthographic 2D cameras, sprite sorting layers, and 2D lighting. A perspective camera fights that pipeline (sorting-layer draw order assumptions, UI/orthographic-tuned art scale, existing `MapCamera.cs` pan/clamp logic) for no real visual gain — the depth illusion here comes from art + parallax + spline, not a physically-tilted camera. Unity version is **6000.4.8f1** if you need to check feature availability.

## What already exists in this project (build on this, don't start from scratch)

All under `Assets/SweetSugar/Scripts/MapScripts/`:

- **`SplineCurve.cs`** — already implements Catmull-Rom-style cubic interpolation in 2D (`GetPoint(a,b,c,d,t)`). Currently only used for **Gizmo drawing**. Useful for sampling arc-length points along the new Sprite Shape road (node placement, movement) — the road's visual rendering itself should go through Sprite Shape (see below), not this.
- **`Path.cs` / `PathMap.cs`** — hold `List<Transform> Waypoints` + a `bool IsCurved` flag that's currently **not doing anything visual at runtime** (only affects gizmo sampling). Nodes are still hand-placed straight-ish Transforms in the editor scene.
- **`WaypointsMover.cs`** — moves the player-icon between two `PathPivot`s. The *curved* movement logic (`UpdateCurved()`, Catmull-Rom waypoint-chasing) **already exists in this file but is commented out** — `Update()` currently does a flat 1-second linear `Vector2.Lerp` between two points. Re-enabling/finishing this is most of the "make movement follow the curve" work.
- **`MapLevel.cs`** — one per node, holds `Number`, `IsLocked`, `PathPivot` (its position on the path), star icons.
- **`LevelsMap.cs`** — singleton orchestrator; finds all `MapLevel`s, orders by `Number`, updates lock/star state, teleports or walks the icon (`TranslationType` enum: `Teleportation` / `Walk`).
- **`MapCamera.cs`** — attached to the map's `Main Camera`. Pure 2D drag-pan (mouse/touch delta -> transform translate, clamped to a `Bounds`). No zoom/rotation/tilt — keep it that way (see caveat above), just extend the bounds/parallax hookup.
- **`BackgroundsForStaticMap.cs`** — a single UI `Image` whose sprite is swapped from a flat array, one big static PNG per 20-level chunk. This appears to be a separate/older background mechanism from the `map_background_01`..`05` `SpriteRenderer`s described in Status above (which are the ones actually wired into `Map details`'s coordinate space) — check whether this script is still live before assuming it's the active background system.
- Map scene is `Assets/SweetSugar/Scenes/gameStatic.unity` — map, board and UI objects coexist in one monolithic scene (toggled active/inactive), camera is Orthographic, size `~17.8`, no rotation.
- Animation clips for map decorations already exist at `Assets/SweetSugar/Animation/Map/` (cloud, tree, ship, house, flower, apple-factory) — reuse these for idle motion on new parallax layer props instead of authoring new clips from scratch. See the [unity-2d-sprite-animation](../unity-2d-sprite-animation/SKILL.md) skill for how to trigger them.

## Remaining plan, now that the pre-built map is reachable

The build-a-Sprite-Shape-road-from-scratch plan below is **not needed for the current look** — the pre-baked `path-N.png` rope art already delivers it. Keep `com.unity.2d.spriteshape` (already in `Packages/manifest.json`, still unused) in mind only if a *future* section needs a curve shape the existing 5 pre-baked art pieces don't cover (a 6th+ biome, a differently-shaped switchback) — authoring new rope art by hand doesn't scale the way a Sprite Shape profile does.

1. **Verify node alignment.** Confirm in the Editor that `LevelsMap > Levels`'s actual clickable `MapLevel` nodes sit on top of `Map details`'s decorative curve nodes, not just in the same broad coordinate range. If they're offset, the fix is repositioning `PathPivot` transforms to match the painted curve, not touching the art.
2. **Decide `Start`/PrePlay UX properly.** It's currently force-disabled as a stopgap (see Status above) — design a real dismiss path (back button, or only show it the very first time) before this ships, otherwise the map is unreachable in a real build the same way it was before today's fix.
3. **Re-enable curved movement** (`WaypointsMover.cs`'s commented-out Catmull-Rom chase logic) only after confirming what curve it should actually follow — the existing code chases `SplineCurve`'s procedural math through `PathPivot` waypoints, which needs those waypoints to already trace the painted rope's shape (see point 1) or the icon will visibly cut corners against the art.
4. **Track down the mystery straight-pink-dot path** from the original screenshots (see Status above) — check `game.unity` — so it's clear whether that's dead/replaced content or something still in use elsewhere.
5. **Vary node styling for special levels**, matching the reference's bonus-wheel node — `MapLevel.cs` currently only handles locked/star state, so this needs a small extension (a node "type"/variant field) rather than being purely an art change.
6. **Optional polish**: a curved-world vertex shader for extra horizon curvature, if wanted beyond what the existing art already provides. Because this project is on the **URP 2D Renderer** (not the general Universal Renderer), verify sorting-layer/material compatibility in a throwaway test scene before wiring it into the real map.
