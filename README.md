# QuickAim — SPT 4.1.3

**SPT 4.1.3 port of BetterArmStamina. All credit goes to the original author.**

- **Original mod:** [goatonabicycle](https://github.com/goatonabicycle/SPT-BetterArmStamina)
- **License:** MIT (unchanged)

This port was uploaded to keep the mod available on SPT 4.1.3. If goatonabicycle asks me to take it down, I will remove it immediately, no questions asked. Same goes if they would rather publish their own 4.1 version — this repo exists only until then.

---

Aim stamina that respects how you are standing.

In Tarkov, holding your sights up drains arm stamina at the same rate whether you are stood upright or lying prone with the gun resting on the ground. This changes that:

| Stance | Default drain | Why |
| --- | --- | --- |
| Standing | 100% | Normal — holding the weapon up unsupported |
| Crouching | 60% | You can brace an elbow on your knee |
| Prone | 30% | The gun is resting on the ground |
| Mounted or bipod | 30% | The weapon is supported by something |

All four are adjustable in the F12 menu, from 10% to 200%.

## Installation

Extract into your **SPT root folder** — the one containing `BepInEx\` and `EscapeFromTarkov_Data\`. Files land in `BepInEx\plugins\QuickAim\`.

**Requires SPT 4.1.3.** Client-only, no server mod.

## What changed in this port

The original works logically as written, so the changes are about how much it costs to run and how it detects the weapon being supported.

**No more per-frame reflection.** The original looked up the private `_inMountedState` field on `MovementContext` by reflection every single frame to tell whether the weapon was mounted. `ProceduralWeaponAnimation` exposes `IsMountedState` as a public property — and that is the object the patch already has in hand, since it reads `IsAiming` from it anyway.

**Bipods are detected directly.** Mounting was inferred from the player's movement state (`EPlayerState.Stationary`) as a fallback. `IsBipodUsed` sits alongside it, which is both cheaper and more accurate — a deployed bipod is now recognised as such rather than guessed at.

**The per-frame path exits early.** `Player.LateUpdate` runs every frame in a raid. The common case — not aiming, and not aiming last frame either — is now two field reads and a return. The original ran a singleton lookup, a reflection call and a frame counter regardless of whether anything was happening.

**The multiplier is written on change, not on a timer.** The original re-checked and corrected the value every 60 frames whether or not anything had moved. It is now written when the stance, aiming state, or support state actually changes.

**Logging is off by default.** The original wrote a status line to the log every 120 frames, permanently. That is now a toggle, default off, and only prints when the drain rate actually changes.

**State resets when a raid starts.** The remembered aiming state was static and never cleared, so it carried from one raid into the next. A stale value would mean the first aim of a new raid looked like "no change" and the multiplier was never applied.

**Fika-safe by construction.** The check for your own player comes first, so other players' stamina — simulated on their own clients — is never touched. The mod also disables itself on a headless client.

## Compatibility

**EasyMounting** works alongside this. It changes how easily you can mount a weapon; this changes what mounting does for your stamina. Different methods, no conflict — if anything they complement each other.

**Other stamina mods** may conflict, since there is only one multiplier to write. If something else is setting it too, expect one of them to win.

**Fika: untested.** The design is right and the guard is in place, but it has not been verified in a live co-op raid, and I would rather say so than claim compatibility I have not confirmed.

## Uninstalling

Delete `BepInEx\plugins\QuickAim\`. Nothing is written to your profile.

## Building

```
dotnet build -c Release -p:SptPath="C:\Path\To\SPT"
```

`SptPath` is the folder containing `BepInEx\` and `EscapeFromTarkov_Data\`. The DLL is copied into `BepInEx\plugins\QuickAim\` automatically.

## AI assistance disclosure

This port was produced with substantial AI assistance. Every game type and member used was verified against the installed `Assembly-CSharp.dll` rather than guessed — including confirming that `IsMountedState` and `IsBipodUsed` exist as public properties on `ProceduralWeaponAnimation`, which is what allowed the reflection to be removed.

## License

MIT, unchanged from upstream.

---

If you'd like to support my work, you can [buy me a coffee](https://ko-fi.com/its_blackhawk) ☕
