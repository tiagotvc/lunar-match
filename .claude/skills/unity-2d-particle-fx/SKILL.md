---
name: unity-2d-particle-fx
description: Author and wire up 2D particle/VFX effects (power-up activations, match/destroy bursts, trails) in this Unity project. Use whenever adding a new visual effect, tweaking an existing one, or making a power-up "feel" more impactful.
---

# 2D Particle/FX authoring (SweetSugar match-3)

Render pipeline: **URP, 2D Renderer** (`Assets/SweetSugar/Settings/SweetSugarURP.asset` -> `SweetSugarURP_Renderer2D.asset`). No `Light2D` is used anywhere — every effect relies on sprite color + additive blending + manual sort order, not lighting. Don't introduce Light2D-dependent effects without checking the rest of the scene tolerates it.

## How effects are stored and triggered

There is **no central FXManager/EffectsManager**. Every script that wants an effect does it inline:

```csharp
Instantiate(Resources.Load("Prefabs/Effects/Firework"), position, rotation);
```

Two prefab libraries under `Assets/SweetSugar/Resources/`:
- `Prefabs/Effects/` — 18 prefabs, the main library (`Firework`, `FireworkPackage`, `FireworkSplashMulticolor`, `CircleExpl`, `PackageExpl`, `ItemExpl`, `_ExplosionAround`, `Replace`, `HitFr`, `NatHit`, `Splash`, `Splash 1`, `Snow`, `Teleport`, `WinTrail`, `ParticlesFall`, `package_anim`).
- `Prefabs/Particles/` — 5 raw particle prefabs (`BubbleExplosion`, `FireworkSplash`, `FireworkSplashSpiral`, `Multicolor_firework`, `StripesEffect`) — building blocks the `Effects/` prefabs above compose.
- `Boosts/` — `area_explosion`, `simple_explosion`, spawned specifically by power-up activation.

Sprite sheets for particle textures live in `Assets/SweetSugar/Textures_png/effects`. Materials live in `Assets/SweetSugar/Materials/` (~35 files, named after their effect: `Star1Add.mat`, `TrailAdd.mat`, `Ring_Add.mat`, etc.) — almost all reference the **same legacy Particle/Additive shader** (GUID `0406db5a14f94604a8c57ccfbc9f3b46`), not a Shader Graph. **Match this convention** for new particle materials unless you have a specific reason to introduce a URP Shader Graph particle shader — mixing shader families risks inconsistent sorting/blending under the 2D Renderer.

## The power-up FX pattern (reference: `Scripts/GUI/Boost/BoostAnimation.cs`)

This is the closest thing to a template for "new power-up effect":

1. `ShowEffect()` instantiates the visual prefab(s) (e.g. `Prefabs/Effects/Firework`, tinted via `SetColor` for area boosts; `Prefabs/Effects/CircleExpl` for `area_explosion`).
2. Effect auto-cleans itself with a hardcoded delay: `Destroy(obj, 1f)` / `Destroy(obj, 0.4f)` — there's no pooling, effects are cheap one-shot bursts.
3. `OnFinished(BoostType)` is called *separately* to apply the actual gameplay result (destroy a 3x3 area, destroy one item, etc.) — **visual and gameplay effect are decoupled**, don't couple them in new code either.

`BoostType` enum (`Scripts/GUI/Boost/BoostShop.cs`): `ExtraMoves, Packages, Stripes, ExtraTime, Bomb, MulticolorCandy, FreeMove, ExplodeArea, Marmalade, None`.

Other real spawn sites worth reading before writing a new one:
- `Scripts/Effects/ExplAround.cs` — radial push (LeanTween moves neighboring items outward) + activates a particle child (`particle.SetActive(true)`) rather than instantiating fresh — use this pattern when the effect needs to interact with existing scene objects, not just play on top.
- `Scripts/Items/ItemDestroyAnimation.cs` `DestroyPackage()` — spawns `_ExplosionAround` at the item's position before destroying it, for the "special piece pops" moment.
- `Scripts/Effects/SplashParticles.cs` — `[ExecuteInEditMode]`, drives a `ParticleSystem`'s texture-sheet-animation frame via `SetColor` to recolor the same particle sheet per match color — reuse this instead of making N colored copies of a particle prefab.
- `Scripts/Effects/Lightning.cs` — a procedural `LineRenderer` bolt, not a `ParticleSystem`. Use this pattern (not particles) for anything that needs to connect two dynamic points.

## 2D sort order (important, gets missed easily)

Every FX prefab should carry `Scripts/ParticleSorting.cs`, which forces `ParticleSystemRenderer.sortingLayerID = 0` with a configurable `sortingOrder` (default `3`). Without it, new particle prefabs render on the default layer/order and can end up behind board pieces or UI. Copy this component onto every new effect prefab and set the order relative to what it needs to draw over/under.

## Recipe: new power-up effect

1. Build the `ParticleSystem` (or reuse a `Particles/` building block) as a new prefab under `Resources/Prefabs/Effects/<Name>`.
2. Add `ParticleSorting` and set `sortingOrder` by comparing to a similar existing effect.
3. Assign a material following the existing `*Add.mat` additive-shader convention (or duplicate one and swap the texture).
4. Trigger it the way `BoostAnimation.ShowEffect()` does: `Instantiate(Resources.Load("Prefabs/Effects/<Name>"), pos, rot)` + `Destroy(obj, duration)`, called from wherever the gameplay event fires (existing `BoostAnimation`/`Item`/`ItemDestroyAnimation` call sites are the right place to hook a new `BoostType`, not a new manager).
5. If it needs to react to match color, follow `SplashParticles.cs`'s `SetColor` pattern instead of duplicating prefabs per color.
