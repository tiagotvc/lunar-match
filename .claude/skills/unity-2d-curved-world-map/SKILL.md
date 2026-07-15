---
name: unity-2d-curved-world-map
description: Build a Candy-Crush-style pseudo-3D curved level-select path/world map for this Unity 2D project. Use when working on the map/path screen, level node layout, map camera, or map background/terrain art.
---

# Curved pseudo-3D world map (SweetSugar level-select screen)

Goal: replace the current flat, mostly-straight dashed path over a static PNG (see current screens: `LevelsMap`/`gameStatic.unity`) with a Candy-Crush-style winding path over terrain art that reads as a curving 3D world, **without leaving 2D/orthographic rendering**.

## The real technique (this genre doesn't use a literal 3D camera)

Games like Candy Crush Saga, Coin Master, Angry Birds 2, etc. do **not** render an actual tilted 3D globe per node. The illusion is built from three layered tricks, combined:

1. **A smooth spline path** (Bezier/Catmull-Rom), not straight segments — nodes are placed by arc-length along the curve, not by hand-eyeballing dot positions.
2. **Forced-perspective terrain art**: the background is hand-painted/authored to already imply hills, elevation and depth (large foreground elements, smaller background elements, hills the path visually climbs over). The path curve is drawn to follow the *painted* terrain shape — the "3D" is baked into the art, not computed by the engine.
3. **Parallax**: background layers (sky/horizon, distant hills, midground, foreground props) scroll at different speeds as the camera pans, which is what actually sells "depth" to the eye far more than any camera trick.

An optional fourth layer of polish is a **curved-world vertex shader** (offsets a ground mesh's Y based on distance-from-camera², the classic Subway Surfers/Temple Run "horizon curves away" effect). It's a nice-to-have for extra polish, not how the core snake-path illusion is made — see caveat below before reaching for it.

**Do not switch the map camera to Perspective projection.** The project's render pipeline is **URP with the 2D Renderer** (`Assets/Settings/Renderer2D.asset`, confirmed via `ProjectSettings/GraphicsSettings.asset`), which is built around orthographic 2D cameras, sprite sorting layers, and 2D lighting. A perspective camera fights that pipeline (sorting-layer draw order assumptions, UI/orthographic-tuned art scale, existing `MapCamera.cs` pan/clamp logic) for no real visual gain — the depth illusion here comes from art + parallax + spline, not a physically-tilted camera. Unity version is **6000.4.8f1** if you need to check feature availability.

## What already exists in this project (build on this, don't start from scratch)

All under `Assets/SweetSugar/Scripts/MapScripts/`:

- **`SplineCurve.cs`** — already implements Catmull-Rom-style cubic interpolation in 2D (`GetPoint(a,b,c,d,t)`). Currently only used for **Gizmo drawing**. This is your curve math — reuse it for actually rendering the path (e.g. feed it into a `LineRenderer` or a custom mesh strip) instead of writing new spline code.
- **`Path.cs` / `PathMap.cs`** — hold `List<Transform> Waypoints` + a `bool IsCurved` flag that's currently **not doing anything visual at runtime** (only affects gizmo sampling). Nodes are still hand-placed straight-ish Transforms in the editor scene.
- **`WaypointsMover.cs`** — moves the player-icon between two `PathPivot`s. The *curved* movement logic (`UpdateCurved()`, Catmull-Rom waypoint-chasing) **already exists in this file but is commented out** — `Update()` currently does a flat 1-second linear `Vector2.Lerp` between two points. Re-enabling/finishing this is most of the "make movement follow the curve" work.
- **`MapLevel.cs`** — one per node, holds `Number`, `IsLocked`, `PathPivot` (its position on the path), star icons.
- **`LevelsMap.cs`** — singleton orchestrator; finds all `MapLevel`s, orders by `Number`, updates lock/star state, teleports or walks the icon (`TranslationType` enum: `Teleportation` / `Walk`).
- **`MapCamera.cs`** — attached to the map's `Main Camera`. Pure 2D drag-pan (mouse/touch delta -> transform translate, clamped to a `Bounds`). No zoom/rotation/tilt — keep it that way (see caveat above), just extend the bounds/parallax hookup.
- **`BackgroundsForStaticMap.cs`** — a single UI `Image` whose sprite is swapped from a flat array, one big static PNG per 20-level chunk (no parallax, no layering). **This is the main thing to replace** for the parallax effect.
- Map scene is `Assets/SweetSugar/Scenes/gameStatic.unity` — map, board and UI objects coexist in one monolithic scene (toggled active/inactive), camera is Orthographic, size `~17.8`, no rotation.
- Animation clips for map decorations already exist at `Assets/SweetSugar/Animation/Map/` (cloud, tree, ship, house, flower, apple-factory) — reuse these for idle motion on new parallax layer props instead of authoring new clips from scratch. See the [unity-2d-sprite-animation](../unity-2d-sprite-animation/SKILL.md) skill for how to trigger them.

## Concrete plan, in dependency order

1. **Finish the curved path rendering.** Use `SplineCurve.GetPoint` to sample points between consecutive `PathPivot`s and render the path as a smooth curve (LineRenderer or a thin mesh strip) instead of straight dashed segments between dots. Reposition/re-space `MapLevel` nodes by arc-length along this curve rather than raw hand-placed Transforms, so spacing stays even as the curve bends.
2. **Re-enable curved movement.** Uncomment/rewrite `WaypointsMover`'s Catmull-Rom chase logic so the walking-icon animation follows the same curve the path is drawn with, instead of a straight-line Lerp.
3. **Replace the flat background with parallax layers.** Split `BackgroundsForStaticMap`'s single big PNG per chunk into 2-4 layers (far sky/horizon, distant hills, midground terrain, foreground props) parented so `MapCamera`'s existing pan delta can be applied to each layer at a different multiplier (far layers move slower). This is the single highest-impact change for the "sophisticated" look the user is after.
4. **Art direction constraint to hand off to whoever paints backgrounds**: the terrain art needs forced perspective (hills/elevation implied by the painting itself) so the spline path drawn on top visually appears to climb over it — the engine-side curve alone won't look "3D" without matching art. Get the reference screenshots the user is planning to send before finalizing background layout, since the exact visual target (how aggressively curved, how many parallax layers, art style) should come from those.
5. **Optional polish (do last, prototype in isolation first)**: a curved-world vertex shader on the terrain layer for extra horizon curvature. Because this project is on the **URP 2D Renderer** (not the general Universal Renderer), verify sorting-layer/material compatibility in a throwaway test scene before wiring it into the real map — 2D Renderer has narrower shader/lighting support than 3D URP and this is the one piece of this plan without precedent already in the codebase.

## Open question to resolve with the user

The user said they'd send reference screenshots of the exact Candy Crush look they want (curve aggressiveness, background art style, node scaling for depth). Steps 1-3 above are safe to start regardless; revisit step 4's art direction once those references arrive.
