# Shadow Unit: Frontline — Phase 1 Prototype

An original mobile first-person tactical shooter built in Unity (C#). Phase 1 delivers a playable offline **Warfare** match: you and 3 AI teammates (Blue) against 4 AI enemies (Red), fighting over three capture points in *the Wasteland Ward*, a grey-box canyon map (see docs/WORLD.md).

No Delta Force assets, names, maps, code or UI are used. Every model, texture and sound in this phase is generated in code as **placeholder** content, to be replaced in Phase 5 (production polish).

> **Verification status.** The scripts were written without access to Unity. They compile cleanly (0 errors, 0 warnings) against stand-in Unity APIs in both input configurations, which checks syntax, types and wiring. They have **not yet been run inside Unity or on a phone**. Expect a short round of tuning and fixes on first play. Report any console errors and they can be fixed quickly.

---

## 1. Requirements

| Item | Version |
|---|---|
| Unity | 2022.3 LTS or Unity 6 (6000.x) |
| Template | **3D (Built-in)** or **3D (URP)**. Both work. |
| Modules | Android Build Support (with OpenJDK, Android SDK & NDK). iOS Build Support optional. |
| Packages | None beyond the defaults (uGUI ships with Unity). |

## 2. Setup (about 5 minutes)

1. Create a new Unity project from the 3D (Built-in) or 3D (URP) template.
2. Copy the `Assets/ShadowUnit` folder from this package into your project's `Assets` folder.
3. Wait for Unity to compile. A **Shadow Unit** menu appears in the top menu bar.
4. **Shadow Unit → Set Input Handling To Both**, then restart Unity.
   (Manual route: *Edit → Project Settings → Player → Other Settings → Active Input Handling = Both*.)
   Touch controls work with any setting; *Both* also enables keyboard and mouse testing in the Editor.
5. **Shadow Unit → Create Prototype Scene**. This creates:
   - `Assets/ShadowUnit/Scenes/Frontline.unity` (one GameObject with `GameBootstrap`), added to Build Settings
   - `Assets/ShadowUnit/Resources/ShadowUnit/SU_Lit.mat`, `SU_Emissive.mat`, `SU_Unlit.mat`, `SU_Sky.mat` (base materials matched to your render pipeline so shaders, including the emissive variant, are included in builds)
   - `Assets/ShadowUnit/Resources/ShadowUnit/Weapons/VK7_Carbine.asset` (the rifle's stats)
6. Press **Play**.

If you later switch render pipeline (Built-in ↔ URP), or you set the project up before `SU_Emissive.mat` existed, run **Shadow Unit → Create Base Assets** again.

No tags, layers, prefabs or scene wiring are required. The map, soldiers, UI and effects are all built at runtime by `GameBootstrap`.

## 3. Controls

**Phone (landscape)**

| Control | Action |
|---|---|
| Left thumb, lower-left area | Floating joystick to move. Push past the ring towards the top to auto-sprint. |
| Right thumb, anywhere on the right side | Drag to look |
| FIRE (big, right) and FIRE (small, left) | Hold to shoot. Dragging while holding also aims the camera. |
| AIM | Toggle aim-down-sights |
| RELOAD, JUMP, CROUCH, SPRINT | As labelled. Crouch and sprint toggle. |
| II (top left), or the Android back button | Pause. Back again resumes. |

**Editor / desktop testing** (needs Active Input Handling = Both): click the right half of the Game view to capture the mouse. Then WASD move, mouse look, left click fire, right click aim, R reload, Space jump, C crouch, Shift sprint, Esc pause and release the mouse.

## 4. What is in Phase 1

### Working in code (compile-checked; needs in-Unity confirmation)

- **Battlefield:** *The Wasteland Ward*, a 140 × 140 m canyon walled by cliffs: the Market District (A) with stacked shops, stalls and the shark-head sign, an Undercity tunnel, the crashed freighter of the Hub (B), the Outer Road with burnt vehicles, a river crossed by two bridges, and the Watchtower (C). Mesas and the Orbital City skyline beyond the cliffs, low warm sun, dusty fog and a procedural sky.
- **Navigation:** a waypoint grid (1.5 m) sampled from the physics scene at load, with A* pathfinding and automatically detected cover positions. It needs no NavMesh package.
- **Player:**
  - CharacterController movement: walk, sprint, crouch (with ceiling check), jump and gravity.
  - Touch look with separate hip and ADS sensitivity, invert Y and optional gyro aiming.
  - Head bob and footsteps.
- **Weapon:**
  - Data-driven `WeaponData` ScriptableObject covering damage, falloff, headshot multiplier, fire rate, magazine, reserve, reload time, hip/ADS/movement spread, bloom, recoil and recovery, ADS FOV and time, pellets, fire mode, and optional custom sounds.
  - Hitscan firing that ignores the shooter's own colliders.
  - Hit markers (white for hits, red for kills and headshots), tracers, muzzle flash with light, and dust/spark or blood impacts.
  - Viewmodel with sway, bob, kick, reload dip and sprint pose. Auto-reload when empty, and a dry-fire click.
- **Health:** damage, a 5-second regeneration delay, spawn protection, directional damage indicators, a low-health warning colour and optional vibration.
- **AI (both teams):**
  - A state machine with Objective, Engage, Cover and Search states.
  - Spotting uses a 124° vision cone, a range that depends on difficulty, and a line-of-sight raycast, so bots never see through walls. Crouching shortens the distance at which you are spotted.
  - Bots hear gunfire within 45 m and search the sound. They also turn towards an attacker when hit.
  - In a fight they use a reaction delay, shoot in bursts with movement-based inaccuracy, reload on a 30-round magazine, seek cover when hurt or reloading, and strafe.
  - When they lose a target they search its last known position, then return to objectives. They path around obstacles, recover when stuck, and keep spacing from teammates.
- **Warfare rules:**
  - Three capture points. Capture speed scales with the number of soldiers inside, and a contested point freezes. Points can be neutralised and flipped.
  - Each held point scores +1 per second and each kill +3. The first team to 300 wins, otherwise the leader when the 7:00 timer runs out wins.
  - Respawn after 5 seconds, either at base or at a quiet held point.
- **UI (runtime uGUI, safe-area aware):**
  - Main menu with an animated fly-over of the map.
  - Settings: sensitivity, ADS sensitivity, invert, gyro, vibration, graphics preset, 60 FPS mode, volume, bot difficulty and controls opacity.
  - **Customize controls:** drag buttons, then save or reset. The layout is stored on the device.
  - HUD: health, ammo, score bars, timer, A/B/C status chips, on-screen objective markers with distance, kill feed, minimap (teammates always shown, enemies only when spotted), dynamic crosshair, reload prompt and respawn countdown.
  - Pause menu and a victory/defeat screen with a scoreboard.
- **Audio:** original synthesised placeholder sounds for gunfire (close and distant), reload, impacts, footsteps, hit and kill markers, damage, captures, victory/defeat, UI clicks and wind ambience. World sounds are 3D-positioned.
- **Performance groundwork:** pooled tracers, flashes, particles and audio voices. Graphics presets (Low/Medium/High) control shadows, shadow distance, anti-aliasing and draw distance, with a 30/60 FPS target.

### Not yet implemented (later phases)

| Feature | Phase |
|---|---|
| More weapons (SMG, sniper, shotgun, LMG, pistol), weapon switching, grenades | 2 |
| Training Ground (firing range, dummies, damage numbers) | 2 |
| Animated, rigged soldier models; better hit reactions | 2 / 5 |
| Operator selection and loadout screen | 2 |
| Operations extraction mode: loot, inventory, extraction | 3 |
| Online multiplayer, accounts, matchmaking, lobbies, server authority | 4 |
| Final art, recorded audio, LODs, occlusion culling setup, resolution scaling | 5 |
| Store assets, device testing matrix, release pipeline | 6 |

The menu shows Training and Operations as disabled buttons, and says plainly that online play is not active.

## 5. Project structure

```
Assets/ShadowUnit/
  Editor/ShadowUnitSetup.cs        menu: create scene + base assets, set input handling
  Scripts/
    Core/      GameBootstrap, GameSettings, Health (+DamageInfo), Soldier, Team,
               Ballistics, MaterialLibrary, AudioLibrary, AudioManager, EffectsManager
    World/     MapBuilder (+MapInfo), WaypointGraph, CapturePoint
    Player/    PlayerController
    Input/     MobileInputController, TouchControl, VirtualJoystick, TouchLookPad, TouchButton
    Weapons/   WeaponData (ScriptableObject), WeaponController, WeaponModels
    Operators/ OperatorData (ScriptableObject): ZOX, Scott, Daed (see docs/OPERATORS.md)
    Levels/    LevelData (ScriptableObject): the 5 campaign levels (see docs/WORLD.md)
    AI/        SoldierAI, SoldierBody
    Match/     MatchManager, SpawnManager
    UI/        UIManager, HUDController, UIFactory, SafeArea
  (created by the setup menu)
  Scenes/Frontline.unity
  Resources/ShadowUnit/SU_Lit.mat, SU_Emissive.mat, SU_Unlit.mat, SU_Sky.mat, Weapons/VK7_Carbine.asset, Hornet9_SMG.asset, Longreach_DMR.asset,
                       Operators/ZOX.asset, Scott.asset, Daed.asset, Levels/Level01-05.asset
```

How the pieces relate:

- `GameBootstrap` builds `AudioManager`, the map (`MapBuilder`, which also builds the `WaypointGraph`), the camera, `EffectsManager`, `MatchManager` and `UIManager`.
- `MatchManager` owns the match state, score and respawns, and spawns the `PlayerController` and the `SoldierAI` bots.
- The player and every bot share the same `Health` and `Soldier` components, so damage, kills and minimap logic are identical for both.
- Gameplay code reads input only from `MobileInputController`. Touch widgets and the desktop keyboard/mouse feed it. That is the seam where a future networked input or gamepad plugs in.
- `UIManager` and `HUDController` only *read* game state and call public methods. No gameplay rules live in the UI.

**Adding a weapon:** Assets → Create → Shadow Unit → Weapon Data, tune the numbers, and assign it to `GameBootstrap.primaryWeapon`. No code changes are needed.

## 6. Testing checklist

1. Play, then confirm the main menu appears with the camera circling the map and no console errors.
2. Press Deploy. You spawn at the south base and bots spread to A, B and C.
3. Fire at a wall to see dust and sparks, a tracer, a muzzle flash and the ammo count dropping, with auto-reload at 0.
4. Hit a red soldier. You should get a white hit marker, a red marker on the kill, a kill-feed entry and Blue score +3.
5. Stand in a ring alone. The chip fills, the flag rises and "BLUE CAPTURED X" appears.
6. Hide behind a wall near enemies. They should not shoot through it, and they should search the area if you fire.
7. Get eliminated. You should see the countdown, respawn and 2.5 s of spawn protection.
8. Pause, change settings and customize controls (drag, save, then restart the app: the layout should persist).
9. Let the timer run out or reach 300, and check the result screen. Play Again should restart cleanly.
10. Repeat with Graphics on Low and 60 FPS on, and watch the Stats window or Profiler.

## 7. Known limitations

- Placeholder visuals: soldiers and weapons are primitive shapes. Hit detection uses each soldier's capsule, and headshots are decided by hit height.
- The waypoint graph is built at load: roughly 8,600 nodes. Expect a short pause on start, longer on low-end phones. It can be baked to an asset later.
- Bots do not vault, climb or use doors tactically beyond pathing through them. Cover choice is local (within 13 m).
- Graphics presets cannot change URP's pipeline asset (render scale, URP shadow settings). Under URP, adjust shadows on the URP asset itself. On the Built-in pipeline the presets apply fully.
- With *Active Input Handling = Input System Package (New)* only, touch works but keyboard and mouse testing is disabled. Use *Both*.
- Gyro aiming uses the legacy Input API, so it needs *Both* or the old input manager. Its axes are mapped for both landscape orientations; confirm the directions on a real phone.
- The map is static-batched at load to cut draw calls. If a mesh-readability error appears in a build, remove the `StaticBatchingUtility.Combine` call at the end of `MapBuilder.Build`.
- Results-screen columns use a proportional font, so alignment is approximate.

## 8. Build and install on Android

1. *File → Build Settings → Android → Switch Platform.* Confirm `Frontline` is ticked at the top of the scene list.
2. *Player Settings*:
   - **Other Settings**:
     - Package Name, e.g. `com.fhg.shadowunit`
     - Minimum API Level 24 or higher
     - Scripting Backend **IL2CPP**
     - Target Architectures **ARM64**
     - Active Input Handling **Both**
   - **Resolution and Presentation:** Landscape Left and Right only (the setup menu already sets this).
3. On your phone: enable Developer options and USB debugging, then connect it by USB and accept the prompt.
4. *Build And Run* installs and launches the game. Alternatively, *Build* to get an `.apk`, then copy it to the phone and install it (allow "install unknown apps").
5. For a Play Store upload later: tick *Build App Bundle (.aab)* and set up a keystore under *Publishing Settings*.

## 9. Next phase (Phase 2) plan

- Weapon roster (SMG, shotgun, sniper, LMG, pistol) as `WeaponData` assets, with weapon switching and frag grenades.
- Training Ground scene with a firing range, pop-up targets and floating damage numbers.
- Operator selection (Nightjar / Anvil / Ember) and a loadout screen.
- Bake the waypoint graph to an asset. Add flanking and squad callouts to the AI.
- Swap in rigged characters and weapons (imported FBX or GLB), with animation controllers.
