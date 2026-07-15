---
name: unity-2d-curved-world-map
description: Warp the level-select map into a Candy-Crush-style curved "globe" world (background + all assets bending together into a horizon) for this Unity 2D project. Use when working on the map's curvature/perspective look, not the path layout itself.
---

# Curved "globe" world map (SweetSugar level-select screen)

**The actual goal (clarified by the user 2026-07-15, correcting an earlier misread of this task):** the winding path with level nodes already exists and is fine — that is *not* what's being asked for here. What's wanted is the **whole map bending like the surface of a globe** as you scroll: the background and every asset on it (path, nodes, trees, houses) warp together into a horizon/perspective curve, the way Candy Crush Saga's world map does, while the game stays fully 2D/orthographic (no real 3D camera).

## The real technique: a shared world-space vertex-offset shader

This is the same family of effect as the "curved world" shaders endless runners (Subway Surfers, Temple Run) and Animal Crossing use for their horizon, adapted from 3D meshes to 2D `SpriteRenderer`s. Confirmed real-world precedent for the sprite case specifically: [Unity Discussions - "Curved horizon with sprites"](https://discussions.unity.com/t/solved-curved-horizon-with-sprites/593089).

**How it works:** every vertex gets displaced in world space by an amount that grows with the *square* of its distance from the camera along the scroll axis. Small near the camera, large near the top/bottom of the screen — that's what reads as "the world curving away," not a flat plane. Because this project's camera is orthographic (confirmed, `MapCamera.cs`), there's no perspective-driven size falloff to fake — the curve has to come entirely from actual position displacement, which is exactly what this shader does.

**Critical requirement: every map element must share the same material/shader**, all computing their offset relative to the same camera position. If only the background uses it, the background will bend while the path/nodes/trees stay flat and visibly detach from it — the user's ask was explicitly "o background... é dobrado juntamente com os assets" (the background bends *together with* the assets). This is a material-swap-and-verify job across `Map details`'s children (`Path`, `Separations`, `Grass`, `Flower`, `Trees anim`, `Buildings`, `map-item-*`, `map_banner_1`) and the `map_background_01`..`05` tiles, not a single-object shader hookup.

## Implementation: `Assets/SweetSugar/Shaders/CurvedWorldSprite.shader`

Already written (2026-07-15), based on the actual installed URP source (`Library/PackageCache/com.unity.render-pipelines.universal@.../Shaders/2D/Sprite-Unlit-Default.shader` and its `Core2D.hlsl`/`2DCommon.hlsl` includes — read directly from disk, not guessed from memory, to minimize the chance of a compile error). It's a near-verbatim copy of URP's real `Sprite-Unlit-Default` shader with one change: the vertex function computes world position, offsets `.x` by `(worldY - cameraY)² * _Curvature`, then transforms to clip space — instead of the stock single-line `TransformObjectToHClip`.

**This has not been compiled/tested in the Unity Editor yet** — I can't run Unity myself, so this needs a live check. To test:
1. Pull the latest commit so the `.shader` file exists on disk, let Unity import it.
2. Create a new Material (`Create > Material`), set its shader to `Custom/CurvedWorldSprite`, tweak `_Curvature` (start small, e.g. `0.01`–`0.05` — it's a squared term so it grows fast).
3. Drag it onto one `SpriteRenderer` first (e.g. `map_background_01`) to confirm it compiles and bends visibly before rolling out to every map element.
4. If it fails to compile, report the exact console error back — the include paths are version-sensitive (this project is on URP `17.4.0` / Unity `6000.4.8f1`, confirmed via `Packages/packages-lock.json`) and may need adjusting for a different URP version.

**Known caveat from the reference implementation:** sprites near the edges of curvature can disappear due to `SpriteRenderer`'s bounds-based frustum culling — Unity culls based on the *original* (undisplaced) bounds, so a heavily-curved sprite can be culled before it visually reaches the edge of the screen. If this shows up, either keep `_Curvature` small enough that displacement stays within the existing bounds margin, or increase each affected `SpriteRenderer`'s bounds/disable frustum culling for the map layer specifically.

**Sorting note:** the offset only touches `.x`, not `.y`/`.z` or sorting order, so it shouldn't disturb the project's existing 2D sorting-layer setup (`ParticleSorting.cs`-style `sortingLayerID`/`sortingOrder`) — but verify draw order still looks right once multiple curved layers overlap, since bending things sideways can change which overlaps which at the edges.

## What already exists in this project (context, not what's being changed here)

The winding path/node system below was investigated and fixed *before* this clarification — it's real, working, and not what this skill is now about, but the context is still useful since the curvature shader has to render on top of it:

- `LevelsMap > Map details` (`Assets/SweetSugar/Scenes/gameStatic.unity`) holds the path art (`path-1.png`..`path-5.png`, pre-baked serpentine rope sprites), decorations (`Grass`, `Flower`, `Trees anim`, `Buildings`, `map-item-*`), and zone banners (`map_banner_1`) — was disabled, now re-enabled.
- `LevelsMap > CanvasMap > SafeArea > Start` (the "Level N, tap to Play" panel, `StaticMapPlay.cs`) was hardcoded always-on with no dismiss button, permanently covering the map — force-disabled as a stopgap so the map is visible/testable; needs real UX (a dismiss button) before shipping.
- `LevelsMap > CanvasMap > SafeArea > Background` was a leftover demo placeholder (bakery art with the asset's original "Candy Smith" watermark — "SweetSugar" is this project's internal rename of that Asset Store template) covering the real map; disabled in favor of the real `map_background_01`..`05` tiles (now enabled).
- `Assets/SweetSugar/Scripts/MapScripts/SplineCurve.cs` (Catmull-Rom curve math, currently gizmo-only) and `WaypointsMover.cs` (commented-out curved movement) are about the *path shape*, unrelated to this globe-warp shader — don't conflate the two tasks.
- `com.unity.2d.spriteshape` (14.0.1) is installed but unused — also unrelated to this specific ask (it's for authoring textured road curves, not a global bend effect).

## Open items

- Roll the shared curvature material out to every `Map details` child + `map_background_01..05`, not just one test object.
- Tune `_Curvature` against the actual reference screenshots the user has (start conservative — this is a squared term, small changes near the edges of the screen get large fast).
- Check whether UI-space elements (HUD icons, banners if any are `Canvas`/`Image` rather than `SpriteRenderer`) need a separate treatment, since this shader is written for `SpriteRenderer`s specifically, not `UnityEngine.UI.Image`.
- Watch for the sprite-culling edge-disappearance caveat above once curvature is live on multiple objects.
