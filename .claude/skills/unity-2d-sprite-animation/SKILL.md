---
name: unity-2d-sprite-animation
description: Animate any sprite in this Unity project (match-3 blocks/items, UI, characters, environment/map decorations). Use whenever adding, changing, or debugging an animation on a GameObject.
---

# Sprite animation (SweetSugar match-3)

Four animation techniques **coexist on purpose** in this codebase — there is no single "AnimationManager". Pick based on what the animation is *for*, don't default to one everywhere.

| Technique | Use it for | Reference files |
|---|---|---|
| Animator + trigger/bool params + PPtr sprite-swap clips | Anything driven by game **state** (a block appearing, being a package, exploding, being tapped) | `Scripts/Items/Item.cs`, `Animation/item_animations/Item.controller` |
| LeanTween (`LeanTween.Framework.LeanTween`) | One-off **procedural** tweens (move/scale/fade/sequence) not tied to a state machine | `Scripts/Items/itemTimeBomb.cs`, `Scripts/Effects/ExplAround.cs` |
| Legacy `Animation` component + Animation Events | GUI popups/banners that need an "on finished" callback | `Scripts/GUI/AnimationEventManager.cs` |
| Manual coroutine + `AnimationCurve` | Bespoke motion needing a custom easing curve Unity's other systems don't give you directly | `Scripts/System/AnimateItems.cs` |

## 1. Animator-driven state animation (the main pattern for game pieces)

`Item.cs` has `[RequireComponent(typeof(Animator))] public Animator anim`. Everything is triggered via `SetTrigger`/`SetBool`/`SetFloat` — never `anim.Play()` directly:

```csharp
anim.SetTrigger("destroy");        // SmoothDestroyCor
anim.SetTrigger("stop");
anim.SetTrigger(anim_name);        // PlayDestroyAnimation(string anim_name)
anim.SetBool("package_idle", true);
anim.SetBool("ingredient_idle", true);
anim.SetFloat("ingredient_offset", value);
```

`Animation/item_animations/Item.controller` is the reference controller: 11 states (`appear`, `disappear`, `destroy`, `destroy_package`, `package_idle`, `tip`, `stop`, `horr_appearing`, `Ingredient_idle`, `AroundExplosion`) wired with `AnimatorStateTransition`s off named trigger/bool params. **To add a new item animation state**: add the state + a transition condition on a new or existing param to this controller, add the corresponding `.anim` clip under `Assets/SweetSugar/Animation/item_animations/`, then call `anim.SetTrigger("<yourParam>")` from the relevant script — same pattern `Square.GenItem()` uses (`anim?.SetTrigger("bonus_appear")`).

The clips themselves are usually flipbook sprite-swaps: a `PPtrCurve` on `m_Sprite` (classID 212) cycling through frames — see `item_anim1.anim` (5 frames @ 15 samples/sec) as a template for a new flipbook animation, rather than keyframing transform properties.

`MainMenu/character_main.controller` and `button_controller.controller` (Unity's standard `Normal/Highlighted/Pressed/Disabled` + custom states off `AnyState`) are the templates for UI/character state machines respectively.

## 2. LeanTween procedural tweens

Namespace is `LeanTween.Framework.LeanTween` (project has its own copy under `Assets/SweetSugar/LeanTween/Framework`, not the Asset Store LeanTween namespace — don't `using` the wrong one). Typical call shape, from `ExplAround.cs`:

```csharp
LeanTween.Framework.LeanTween.move(target, endPos, duration).setEase(...);
LeanTween.Framework.LeanTween.sequence()... 
LeanTween.Framework.LeanTween.delayedCall(delay, () => { ... });
```

Use this for anything that's a one-shot tween not worth a whole Animator state — pulsing a label (`itemTimeBomb.cs`), pushing neighbors outward (`ExplAround.cs`), etc.

## 3. Legacy `Animation` component (GUI popups only)

`AnimationEventManager.cs` plays clips by name off a plain `Animation` component (not `Animator`) and relies on **Animation Events** calling back into `OnFinished()`:

```csharp
GetComponent<Animation>().Play();
GetComponent<Animation>()["bannerFailed"].speed = ...;
```

This is an older mechanism kept for the popup/banner system (`banner`, `bannerComplete`, `bannerFailed`, `leadboard_show`, `popupText`, `flyupText`). Extend it if you're adding another popup in this family; don't start new features on it — Animator is the modern default for everything else (see §1).

## 4. Manual coroutine + AnimationCurve

`Scripts/System/AnimateItems.cs` moves/scales/rotates a spawned `SpriteRenderer` by sampling an `AnimationCurve` against `Time.time` inside a coroutine — used for the ingredient "fly to HUD" animation. Reach for this only when you need curve control that neither Animator nor LeanTween's built-in eases give you.

## Animation clip folders (`Assets/SweetSugar/Animation/`)

- Root + `item_animations/` — block/item/package clips (appear, disappear, destroy, explosions, boosts)
- Root + `MainMenu/` — UI buttons, popups, character/avatar clips
- `Map/` — environment decoration clips for the level-select map (clouds, trees, ships, house animations) — see the [unity-2d-curved-world-map](../unity-2d-curved-world-map/SKILL.md) skill if you're animating map terrain/parallax layers specifically
- `Timeline/PackageAnimation/`, `Timeline/Teleport/` — single-frame sprite-swap clips driven by `.playable` Timeline assets (`TeleportDirector.cs`), not Animator — a fifth, narrower mechanism used only for those two sequences

Put new clips in the folder matching what they animate, following the existing naming style (lowercase-with-underscores or PascalCase matching neighbors in that folder).
