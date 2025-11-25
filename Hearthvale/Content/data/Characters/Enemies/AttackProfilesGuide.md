# Attack Profiles Guide

This guide explains the constants used in `AttackProfiles.json`. This file controls the "feel" and physics of every attack, including timing, weapon movement, and collision shapes.

## ⏱️ Timing & Frames
These fields control the rhythm of the attack. Frame numbers are **1-based** to match Aseprite/sprite tooling.

*   **`activeStartFrame`**: The exact frame number where the weapon becomes "dangerous" (hitbox turns on).
*   **`activeFrameCount`**: How many frames the hitbox stays active.
*   **`setupFrameCount`**: The "Wind-up" phase. Number of frames before the hit starts. Used to calculate total duration if `activeStartFrame` isn't explicit.
*   **`recoveryFrameCount`**: The "Cooldown" phase. Number of frames the character is stuck in the animation after the hit finishes.
*   **`durationScale`**: A multiplier for the total speed.
    *   `1.0` = Normal speed.
    *   `1.2` = 20% Slower (heavier feel).
    *   `0.8` = 20% Faster (snappier feel).

## ⚔️ Weapon Movement (Visuals)
These control how the weapon sprite rotates during the swing.

*   **`windUpAngleDegrees`**: How far back the weapon cocks before swinging (negative rotation).
*   **`slashArcDegrees`**: The total arc the weapon travels during the active frames. Larger numbers = wider cleave.
*   **`recoveryAngleDegrees`**: How far the weapon overswings or rests after the slash before returning to idle.

## 📏 Range & Reach
These determine how the AI calculates distance and how big the weapon feels.

*   **`minRange`**: The minimum distance the AI tries to maintain.
*   **`rangeBuffer`**: Extra "padding" added to the calculated reach. Higher values make the AI attack from slightly further away.
*   **`weaponLengthScale`**: Multiplier for the visual weapon sprite size when calculating hitboxes.
    *   `0.8` = Hitbox is smaller than the sprite (precision).
    *   `1.2` = Hitbox is larger than the sprite (generous).

## 💥 Offensive Shape (`shape`)
Defines the **Red** hitbox that deals damage.

*   **`type`**: The geometric shape of the attack.
    *   `"Arc"`: A cone/pie-slice shape (swords, axes).
    *   `"Box"`: A rectangle (hammers, punches).
    *   `"Thrust"`: A narrow line or rectangle extending forward (spears).
*   **`length`**: How far forward the shape extends.
*   **`width` / `thickness`**: How wide the shape is.
*   **`forwardOffset`**: Shifts the shape forward/backward relative to the character center.
*   **`verticalOffset`**: Shifts the shape up/down (useful for overheads or low sweeps).

## 🛡️ Defensive Shape (`defensiveBodyShape`)
**[New Feature]** Defines the **Gold** collision box that prevents the character from walking into walls during the animation.

*   **`type`**: Almost always `"Box"`.
*   **`width`**: The width of the safe collision area.
*   **`height`**: The height of the safe collision area.
*   **`forwardOffset`**: Critical for "lunging" attacks. If a sprite leans forward, shift this positive (+) to cover the new body position.
*   **`verticalOffset`**: Shifts the box up/down.

## ✨ Magic (`magic`)
Optional field for attacks that trigger special effects.

*   **`type`**: e.g., `"AreaOfEffect"`.
*   **`effectId`**: The ID of the particle/logic effect to spawn.
*   **`radius`**: Size of the effect.
*   **`damageScale`**: Multiplier for the damage dealt by this magic effect relative to the character's base power.