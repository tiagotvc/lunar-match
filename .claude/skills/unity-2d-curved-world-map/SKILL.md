---
name: unity-2d-curved-world-map
description: Build a Candy-Crush-style pseudo-3D curved level-select path/world map for this Unity 2D project. Use when working on the map/path screen, level node layout, map camera, or map background/terrain art.
---

# Curved pseudo-3D world map (SweetSugar level-select screen)

Goal: replace the current **completely straight** path (confirmed with the user: "zero curva" — no curve at all today, just a near-vertical line of dots) with a Candy-Crush-style tight winding/switchback road — where the road surface itself is a continuous textured ribbon, not a thin dashed line connecting dots — that reads as a rich 3D world, **without leaving 2D/orthographic rendering**.

Real Candy Crush Saga reference (screenshot provided by the user, phone-mockup, "Lemon Lake" section): the path is a **tight serpentine spiral** (much tighter than gentle S-wobble — real switchbacks), rendered as a continuous purple stone/cobblestone road texture that the camera scrolls along vertically, with gold star-bordered level nodes sitting on top of it, trees/shop buildings/banners placed beside the road, and at least one node styled differently (a bonus color-wheel icon instead of a plain node) to break up visual monotony.

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
- **`BackgroundsForStaticMap.cs`** — a single UI `Image` whose sprite is swapped from a flat array, one big static PNG per 20-level chunk (no parallax, no layering). **This is the main thing to replace** for the parallax effect.
- Map scene is `Assets/SweetSugar/Scenes/gameStatic.unity` — map, board and UI objects coexist in one monolithic scene (toggled active/inactive), camera is Orthographic, size `~17.8`, no rotation.
- Animation clips for map decorations already exist at `Assets/SweetSugar/Animation/Map/` (cloud, tree, ship, house, flower, apple-factory) — reuse these for idle motion on new parallax layer props instead of authoring new clips from scratch. See the [unity-2d-sprite-animation](../unity-2d-sprite-animation/SKILL.md) skill for how to trigger them.

## Concrete plan, in dependency order

1. **Build the road as a Sprite Shape, not a dot-connector.** Add a `SpriteShapeController` + `SpriteShapeRenderer` for the path, author a `SpriteShapeProfile` whose edge sprite is the road/ground texture (cobblestone, sand, etc. — matches the biome), and place its spline control points to form tight switchbacks like the reference screenshot, not a gentle wobble. This replaces the current dashed-line-between-dots rendering entirely. `SplineCurve.cs`'s existing Catmull-Rom math can still be reused for *sampling points along* the authored Sprite Shape spline (for node placement and movement, next steps) even though Sprite Shape handles the actual road rendering/texturing itself.
2. **Re-space `MapLevel` nodes by arc-length along the Sprite Shape spline.** Nodes are currently hand-placed `Transform`s (`PathPivot`) — once the road is a real spline, sample points evenly along its length (reusing `SplineCurve.GetPoint`) so node spacing stays visually even through the tight curves, matching how gold nodes sit evenly along the curve in the reference.
3. **Re-enable curved movement.** Uncomment/rewrite `WaypointsMover`'s Catmull-Rom chase logic so the walking-icon animation follows the same spline the road is built from, instead of the current flat straight-line `Vector2.Lerp`.
4. **Replace the flat background with parallax + decorative props.** Split `BackgroundsForStaticMap`'s single big PNG per chunk into layers (far sky/horizon, midground terrain, foreground props) panned at different multipliers via `MapCamera`'s existing drag delta, and scatter decorative sprites beside the road (trees, shop stalls, banners — matching `apple-factory`/`house`/`tree`/`ship` assets already in `Animation/Map/`) the way the reference screenshot surrounds its road with scenery rather than leaving empty ground.
5. **Vary node styling for special levels.** The reference shows at least one node replaced with a distinct icon (bonus wheel) rather than a plain numbered circle — `MapLevel.cs` currently only handles locked/star state, so this needs a small extension (a node "type"/variant field) rather than being purely an art change.
6. **Optional polish (do last, prototype in isolation first)**: a curved-world vertex shader on the terrain layer for extra horizon curvature beyond what the Sprite Shape road + parallax already provide. Because this project is on the **URP 2D Renderer** (not the general Universal Renderer), verify sorting-layer/material compatibility in a throwaway test scene before wiring it into the real map.
