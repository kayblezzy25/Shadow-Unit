# Shadow Unit — Game Roadmap & Build Prompts

**Unity edition** · Oct 10, 2026 · @Michael Arewa

## Overview

Shadow Unit is an original, offline tactical shooter inspired by the style of modern tactical shooters. It is built in **Unity (C#)** with a **first-person camera**, three playable operators and a set of story levels. Launch target: 5 levels, 3 operators, 5 guns, playable with no Wi-Fi or data, on Android and PC.

**Where the project is now.** The Unity prototype (*Shadow Unit: Frontline*) already has first-person movement and shooting, smart bots, a capture-point Warfare mode, menus, a HUD and touch controls. That covers most of Phase 2 and parts of Phase 5. Steps below say **Already built** where the prototype covers them, so you only prompt for what is missing.

**How to use this doc**

1. Paste the **Master context prompt** at the start of every new AI chat (Claude, ChatGPT, any coding agent).
2. Work through the steps in order. Each step has a goal, a copy-paste prompt and a "done when" check.
3. Don't start the next step until the current one passes its "done when" check.
4. Replace anything in [square brackets] with your own details before sending.
5. When Unity shows red errors in the **Console** window, copy the exact text and paste it into the chat.

**Originality rules (keep it your own game)**

- Borrow the *style*: tactical squads, operators with abilities, extraction runs, seasonal updates.
- Never use their *content*: no "Delta Force", "Haavk", "Ahsarah", "Black Hawk Down", their operator names, map names or logos.
- Use made-up or generic gun names ("AR-7 Rifle", "VK-7 Carbine"), not trademarked ones like M4A1 or MCX.
- Only use assets with a licence that allows it: CC0, CC-BY (credit the author), Unity Asset Store licences, or your own.

## Roadmap at a glance

| Phase | Weeks | Steps | Gate to pass before moving on | Status |
|---|---|---|---|---|
| 1 · Design | 1 | 1–3: game pitch, operators, level plan | Plan on paper | To do |
| 2 · Core gameplay | 2–4 | 4–6: project setup, movement and shooting, enemy AI | **Fun with grey boxes** | Mostly built |
| 3 · Systems | 5–6 | 7–8: weapons, attachments, abilities, offline saving | Saves work offline | 3 guns built |
| 4 · Visuals and feel | 7–10 | 9–11: realistic art and lighting, effects and sound, destruction | Looks like a real place | Placeholder art |
| 5 · UI, builds, polish | 11–12 | 12–15: menus and HUD, offline builds, phone speed, playtest | 60 fps in airplane mode | Menus/HUD built |
| 6 · Launch and seasons | 13+ | 16: store launch, then a new season every 3 months or so | Live on a store | To do |

The most important gate is Phase 2: if the game isn't fun with grey boxes, fix that before spending weeks on art. Timings assume solo part-time work and will stretch or shrink with your schedule.

## Master context prompt

Paste this first in every new AI session, then paste the step prompt underneath it. It stops the AI forgetting your engine, camera and offline rule.

```text
You are my senior Unity game developer and technical artist. We are building my
original game "Shadow Unit".

GAME FACTS (never change these unless I say so):
- Genre: tactical military shooter, single-player, mission-based levels plus an
  offline Battle Mode (me + AI squad vs AI capturing points).
- Inspiration: the STYLE of modern tactical shooters (operators with abilities,
  squads, extraction runs). All names, story, maps and characters must be 100%
  original. Never use Delta Force names, factions, maps or trademarked gun names.
- Engine: Unity [2022.3 LTS / Unity 6], C#, [Built-in / URP] render pipeline.
  Physics: Unity PhysX. Player and bots use CharacterController.
- Camera: first-person, with a weapon viewmodel and aim-down-sights.
- Controls: PC = WASD move + mouse look + left click fire + right click aim.
  Mobile = floating left joystick to move, drag on the right half to look,
  on-screen FIRE / AIM / RELOAD / JUMP / CROUCH / SPRINT buttons.
  All gameplay reads input from MobileInputController.
- Must run 100% OFFLINE: no network calls, no online services, no analytics or
  ads packages. Saves go to a JSON file in Application.persistentDataPath
  (settings may use PlayerPrefs).
- Targets: 60 fps on a mid-range Android phone (steady 30 on Low). Builds:
  Android APK/AAB and Windows PC.
- Look: realistic PBR materials, HDRI sky lighting, baked lightmaps,
  post-processing. Not cartoon, not flat.
- Operators (OperatorData assets, see docs/OPERATORS.md): ZOX - Assault,
  Scott - Support, Daed - Recon.
- Existing code (namespace ShadowUnit, folder Assets/ShadowUnit/Scripts):
  GameBootstrap, GameSettings, Health, Soldier, Ballistics, AudioManager,
  EffectsManager, MaterialLibrary, MapBuilder, WaypointGraph, CapturePoint,
  PlayerController, WeaponController, WeaponData (ScriptableObject), SoldierAI,
  SoldierBody, MatchManager, SpawnManager, UIManager, HUDController,
  MobileInputController and touch controls. Extend these; don't replace them
  unless I ask.

HOW TO ANSWER:
- Give complete, working C# scripts, one file per script, with the file path
  on top (e.g. Assets/ShadowUnit/Scripts/Weapons/WeaponManager.cs).
- Tell me exactly which GameObject or prefab to add each script to, and which
  Inspector fields to set.
- Keep tunable numbers in ScriptableObjects or [SerializeField] fields, never
  hard-coded deep in logic.
- Say which Unity packages (Package Manager) are needed, if any.
- Explain in short, plain steps. I will test in Play mode after each step and
  paste any Console errors back to you.
- If something is unclear, ask me ONE question before writing code.
```

## Phase 1: Design (week 1)

Phase 1 produces three paper documents: the game pitch, the operator sheet and the level plan. No code yet.

### Step 1: Game pitch

Goal: one page that fixes the name, setting, core loop and modes.

```text
Using the game facts above, write a one-page game pitch for Shadow Unit with:
1. A 2-sentence hook.
2. An original setting: fictional country, year, the enemy faction and what
   they want. Give me 3 options to choose from.
3. The core loop in one sentence (pick operator -> deploy -> objectives ->
   extract -> unlock gear).
4. Three offline modes: Campaign (story levels), Extraction Run (solo
   loot-and-escape), Battle Mode (me + AI squad vs AI capturing points).
   Battle Mode already exists as a prototype (Warfare at "Kessel Crossing").
   Mark Campaign as the launch mode and Extraction Run as post-launch.
5. Five words that describe the feel of the game.
```

Done when: you have picked a setting and the pitch fits on one page.

### Step 2: Operators

Goal: three operators, each with a class, one active ability and one passive perk.

```text
Design my 3 operators for Shadow Unit using my character sheets: [describe each
character's look in one line].
For each one give: code name, class (Assault / Support / Recon), short backstory
(3 lines), one ACTIVE ability with cooldown in seconds, one PASSIVE perk,
starting weapon, and a strength + weakness.
Balance them so no operator is best at everything. Present it as a table, then
write OperatorData.cs (a ScriptableObject with these fields) and tell me how to
create the 3 operator assets in Assets/ShadowUnit/Resources/ShadowUnit/Operators.
```

Done when: the 3 operator assets exist and each operator plays differently on paper.

### Step 3: Level plan

Goal: 5 levels with themes, objectives, rising difficulty and a boss on level 3 and level 5.

```text
Plan 5 campaign levels for Shadow Unit. For each level give: name,
theme/location, story reason, 2-3 objectives (rescue, destroy, steal intel,
defend, escort), enemy count and types, boss (levels 3 and 5 only), extraction
point, estimated play time (5-10 min), and one "wow" moment (a collapse,
explosion or ambush).
Then draw each level as a simple ASCII top-down grid (20x20) showing spawn (S),
objectives (O), cover (#), enemies (E), boss (B) and extraction (X).
Finish with LevelData.cs (ScriptableObject: objectives, enemy spawns, boss,
extraction point) and the values for all 5 level assets.
```

Done when: all 5 levels are sketched and the 5 level assets exist.

## Phase 2: Core gameplay (weeks 2-4)

Phase 2 makes the game fun with grey boxes only. If shooting and AI feel good with boxes, they will feel great with art.

### Step 4: Project setup

Goal: a clean Unity project with the prototype running and a test range scene.

**Already built:** the *Shadow Unit → Create Prototype Scene* menu sets up the scene, materials, input handling and build settings. Follow the prototype README, press Play and fix any Console errors first.

```text
Using my existing Shadow Unit Unity project:
1. Suggest the folder structure for new content: Assets/ShadowUnit/ Models,
   Textures, Audio, Prefabs, Scenes, Data (ScriptableObjects), UI.
2. List the scenes to add: MainMenu, Loadout, Level01-Level05, TestRange.
   Explain how to load scenes with SceneManager while GameBootstrap still
   builds the managers, and which objects should use DontDestroyOnLoad.
3. Build a TestRange grey-box scene: 40x40 floor, 10 box walls, 5 sandbag
   blocks, 5 static target dummies with Health, and a player spawn.
4. Add a "Test Range" button to the main menu that loads it.
```

Done when: Play mode runs with no Console errors and you can walk around the test range.

### Step 5: Player movement, aiming and shooting

Goal: the core feel of the game.

**Already built:** walk, sprint, crouch (smaller hitbox), jump, touch and mouse look, hitscan rifle with spread, recoil, reload and aim-down-sights, health with regeneration, tracers, muzzle flash and impacts.

```text
Extend my Shadow Unit player (PlayerController.cs, WeaponController.cs,
Health.cs):
1. Slide: sprint + crouch starts a short slide with speed decay and a lower
   camera; no sliding on steep slopes.
2. Armour: add armour to Health.cs that absorbs a share of damage before
   health; show it on the HUD under the health bar.
3. Hit feel: small camera kick per shot, stronger kick when aiming is off.
4. Give me a short tuning checklist (fire rate, recoil, spread, ADS time) and
   which WeaponData fields change each one.
```

Done when: you can run, slide, aim and shoot targets, and it already feels good.

### Step 6: Enemy AI

Goal: enemies that patrol, react, take cover and hunt you.

**Already built:** a state machine (Objective, Engage, Cover, Search) with vision cone and line of sight, hearing gunfire, reaction time, burst fire, cover seeking and waypoint pathfinding (A*). Difficulty: Easy / Normal / Hard.

```text
Extend my SoldierAI.cs for campaign levels:
1. PATROL state: walk a list of patrol points (Transforms set in the
   Inspector), pause at each, look around.
2. Alert spreading: an alerted enemy alerts allies within a radius
   (Inspector field), with a short delay.
3. Flanking: when the player stays behind the same cover for 5+ seconds, one
   enemy picks a path to the player's side using WaypointGraph.
4. BossAI.cs: extra health, armour phases at 66% and 33%, a special attack,
   and 2-4 guards who protect it.
5. Difficulty fields: reaction time, accuracy, aggression, in a
   DifficultyData ScriptableObject.
Keep using WaypointGraph (no NavMesh) unless you explain why I should switch to
the AI Navigation package.
```

Done when: 5 enemies in the test range patrol, spot you, flank around walls and lose track of you if you hide.

## Phase 3: Systems (weeks 5-6)

Phase 3 adds the weapons, abilities and offline saving that turn a test range into a real game.

### Step 7: Weapons, attachments and operator abilities

Goal: 5 guns driven by data, attachments, and the 3 operator abilities working.

**Already built:** WeaponData ScriptableObject (damage, falloff, fire rate, magazine, reload, spread, recoil, pellets, ADS) and one gun, the VK-7 Carbine.

```text
For Shadow Unit, build the weapon and ability systems in Unity:
1. The Hornet-9 SMG, VK-7 Carbine and Longreach DMR WeaponData assets exist
   (docs/WEAPONS.md). Add a pistol and a shotgun to make 5, with numbers that
   fit alongside them.
2. WeaponManager.cs: equips primary + secondary, swap with Q / a mobile SWAP
   button, keeps ammo per gun, and swaps the viewmodel.
3. Attachments as AttachmentData ScriptableObjects that modify stats: red-dot,
   4x scope, suppressor, extended mag, grip. Show how they stack.
4. AbilityController.cs: implements the 3 abilities from my OperatorData
   assets [paste operator table], with cooldowns, a HUD-ready cooldown value,
   and E / a mobile ABILITY button.
5. Grenades: physics throw arc with a trajectory preview line (LineRenderer),
   explosion radius damage with falloff, and a mobile GRENADE button.
```

Done when: every gun feels different and all 3 abilities work in the test range.

### Step 8: Progression and offline saving

Goal: cash, unlocks and saves that work with no internet.

```text
Write the progression and save system for Shadow Unit (fully offline):
1. SaveSystem.cs: save/load a JSON file in Application.persistentDataPath
   (JsonUtility), write to a temp file then replace, keep one backup. Save
   unlocked levels, cash, owned weapons and attachments, operator XP/levels.
   Include a version number and a safe migration if the save format changes.
   Settings can stay in PlayerPrefs (GameSettings.cs).
2. Rewards: completing a level gives cash + XP; bonus for no damage, speed and
   all objectives.
3. Shop logic: buy weapons/attachments with cash, prices from WeaponData.
4. LevelManager.cs: loads a level from its LevelData asset, tracks objectives,
   triggers extraction when objectives are done, shows win/lose.
5. Extraction Run rule (for later): loot carried is lost on death, kept on
   extraction.
```

Done when: you finish a level, close the game with Wi-Fi off, reopen it and your progress is still there.

## Phase 4: Realistic visuals and feel (weeks 7-10)

Phase 4 replaces the grey boxes with realistic art, lighting, effects and sound. This is where it stops looking like a prototype.

### Step 9: Realistic art and lighting

Goal: PBR characters and environments under realistic light.

```text
Help me make Shadow Unit look realistic in Unity ([Built-in / URP]).
1. Characters: give me a pipeline from my 2D character sheets -> AI
   image-to-3D model -> cleanup in Blender (under 15k triangles for mobile, fix
   normals, UVs) -> rig and animate in Mixamo (idle, run, crouch-walk, aim,
   shoot, reload, die) -> export FBX -> import into Unity as Humanoid with an
   Animator Controller. Write the Animator setup and SoldierAnimator.cs that
   replaces SoldierBody's procedural animation and blends run/aim/shoot.
   Include first-person arms for the viewmodel.
2. Environment: list the CC0 asset types I need from Poly Haven / ambientCG /
   Sketchfab (CC0 or CC-BY) for a [level theme] level, and how to set up URP
   Lit (or Standard) materials: albedo, normal, metallic/smoothness, occlusion.
   Explain how to replace MapBuilder primitives with prefabs.
3. Lighting: HDRI skybox + environment lighting, one directional sun with soft
   shadows (cascade settings for first-person), Light Probes, and baked
   lightmaps for static meshes. Give exact Lighting window settings.
4. Post-processing (URP Volume): ACES tone mapping, bloom, SSAO, colour
   adjustments (warm, dusty, slightly desaturated), vignette. Tie it to my
   Low/Medium/High graphics setting.
```

Done when: a screenshot of the test range looks like a real place, not a cartoon.

### Step 10: Effects, sound and game feel

Goal: every shot, hit and explosion feels powerful.

**Already built:** pooled muzzle flashes, tracers and impact particles, hit markers, damage direction indicators, synthesised placeholder sounds with 3D positioning.

```text
Add game feel to Shadow Unit in Unity:
1. Particle Systems (prefabs, pooled through EffectsManager): muzzle flash,
   dust impact, blood-free hit spark (keep it teen-rated), smoke grenade,
   explosion, shell casings.
2. Decals: bullet holes and scorch marks that fade out, capped at 50 (URP
   Decal Projector, or quads on the Built-in pipeline).
3. CameraShake.cs: small kick when firing, big shake on explosions, scaled by
   distance.
4. Hit feedback: red screen-edge flash on damage, enemy stagger.
5. AudioManager: an AudioMixer with Music/SFX/UI groups, footsteps by surface,
   reload sounds, ambient wind loop, radio voice lines on objectives. Replace
   the AudioLibrary placeholders with imported clips (OGG/WAV in Assets).
List free sources: Freesound.org, Sonniss GDC bundles; voice lines from
ElevenLabs.
```

Done when: shooting feels punchy even with your eyes half closed.

### Step 11: Destruction moments

Goal: one or two scripted "wow" moments per level.

```text
Create a cheap destruction system for Shadow Unit (mobile-friendly):
1. Destructible.cs: an object with Health; on death swap the intact prefab for
   a pre-broken prefab, give the rubble Rigidbodies for 3 seconds, dust
   particles, a big sound and camera shake, then make the debris kinematic to
   save performance.
2. Scripted collapse: a trigger collider that drops a wall or bridge when the
   player or a grenade activates it, and updates enemy paths (rebuild the
   affected WaypointGraph region).
3. Explosive barrels that chain-react.
```

Done when: blowing up a wall in level 1 makes you go "whoa".

## Phase 5: UI, builds, optimisation and testing (weeks 11-12)

Phase 5 wraps the game in menus, makes it install and run with no internet, and gets it smooth on phones.

### Step 12: Menus and HUD

Goal: a complete front end and in-game HUD.

**Already built (uGUI, safe-area aware):** main menu with fly-over, settings, pause, results screen, customisable touch layout; HUD with health, ammo, score, timer, objective markers, kill feed, minimap and damage direction.

```text
Extend the Shadow Unit UI (uGUI, built in code by UIManager/HUDController):
1. Main menu: Campaign, Battle Mode, Operators, Armoury, Settings.
2. Loadout screen: choose operator, primary, secondary, attachments; show
   stats bars from WeaponData.
3. HUD additions: armour bar, ability cooldown ring, grenade count, objective
   text for campaign levels.
4. Mobile controls: add ABILITY, GRENADE and SWAP buttons to the existing
   customisable layout.
5. Campaign win and lose screens with rewards.
```

Done when: someone new can start the game and play level 1 with no help.

### Step 13: Make it fully offline

Goal: installable on PC and Android, working with airplane mode on.

```text
Make Shadow Unit run 100% offline and build it:
1. Check Package Manager and Project Settings > Services: remove or disable
   Unity Analytics, Ads, Cloud Diagnostics and anything that calls the
   network. Turn off Unity's own analytics where the licence allows.
2. Android: step-by-step Build Settings and Player Settings (IL2CPP, ARM64,
   minimum API, landscape only, keystore) to build an APK for testing and an
   AAB for Google Play. Keep screen on.
3. PC: a Windows build (exclusive fullscreen option, resolution settings).
4. Test checklist: install, turn on airplane mode, play a level, save,
   restart, check progress.
```

Done when: the game installs and plays a full level in airplane mode on PC and Android.

### Step 14: Optimise for phones

Goal: steady 60 fps on a mid-range Android phone.

```text
Optimise Shadow Unit for mobile in Unity. Current numbers: [fps, batches/draw
calls, triangle count, build size from the Profiler and Frame Debugger].
1. Texture compression: ASTC for Android; max 2K, 1K on Low.
2. Batching: static batching for level meshes, GPU instancing or SRP Batcher;
   LOD Groups for characters and props; occlusion culling bake.
3. Limit real-time shadows to the player area; rely on lightmaps.
4. Object pooling for bullets, particles, decals, enemies.
5. Audio import settings (compressed in memory, streaming for music) and
   loading each level as its own scene.
6. A Low/Medium/High preset that changes render scale (URP asset), shadows,
   post-processing and particle counts.
Give me a target budget (draw calls, triangles, texture memory) and how to
measure each with the Unity Profiler and Frame Debugger on a connected phone.
```

Done when: level 1 holds 60 fps (or a steady 30 on Low) on your test phone.

### Step 15: Playtest and polish

Goal: find what is confusing, boring or broken.

```text
Create a playtest plan for Shadow Unit:
1. A 10-question feedback form for 5 testers (fun, difficulty, controls,
   confusion points, favourite moment).
2. What to watch for silently while they play.
3. A bug report template (include device, Unity Console error text, steps).
4. Based on this feedback: [paste notes], tell me which numbers to change in
   the WeaponData, OperatorData and DifficultyData assets, ranked by impact.
```

Done when: 3 out of 5 testers want to play another level.

## Phase 6: Launch and seasons (week 13 onward)

Phase 6 ships version 1.0 and keeps it alive with a content season roughly every 3 months.

### Step 16a: Launch

Goal: Shadow Unit 1.0 published with a store page and launch content.

```text
Prepare the launch of Shadow Unit 1.0 (offline tactical shooter, 5 levels,
3 operators, Unity, Android + PC):
1. Google Play listing: title, short description (80 chars), full
   description, 5 screenshot captions, content rating answers, privacy policy
   text (no data collected, works offline).
2. itch.io page copy for the PC version.
3. A 30-second trailer shot list and script.
4. A 2-week launch content plan for TikTok, YouTube Shorts and Instagram:
   devlog clips, before/after graphics, operator reveals.
```

Done when: the game is live on at least one store and the first 5 posts are scheduled.

### Step 16b: Seasonal updates

Goal: a repeatable season formula that also feeds your content channel.

```text
Plan Season [number] for Shadow Unit. Current content: [list levels,
operators, guns].
Give me: a season name and theme, 1 new operator (class, ability, passive),
2 new levels, 2 new guns, 1 new mode or mechanic, a season reward track, and a
6-week dev schedule.
Then a content calendar of 8 posts teasing the season.
Keep everything original and offline-compatible.
```

Done when: the season ships and you have a template to reuse for the next one.

## Progress checklist

- [ ] Step 1: Game pitch done, setting chosen
- [x] Step 2: 3 operator assets done *(ZOX, Scott, Daed: see docs/OPERATORS.md)*
- [ ] Step 3: 5 levels sketched, 5 level assets done
- [ ] Step 4: Prototype runs with no Console errors; TestRange scene added
- [ ] Step 5: Movement, aiming and shooting feel good *(base built; slide and armour to add)*
- [ ] Step 6: Enemy AI patrols, attacks, searches and flanks *(engage/cover/search built; patrol, flanking, boss to add)*
- [ ] Step 7: 5 guns, attachments and 3 abilities working *(3 guns built: Hornet-9, VK-7, Longreach; see docs/WEAPONS.md)*
- [ ] Step 8: Progress saves and loads offline
- [ ] Step 9: Realistic art and lighting in place
- [ ] Step 10: Effects and sound feel punchy *(placeholders built)*
- [ ] Step 11: One destruction moment per level
- [ ] Step 12: Menus, HUD and mobile controls done *(base built; loadout and campaign screens to add)*
- [ ] Step 13: Plays in airplane mode on PC and Android
- [ ] Step 14: 60 fps on test phone
- [ ] Step 15: Playtested with 5 people and balanced
- [ ] Step 16a: Launched
- [ ] Step 16b: Season 1 planned

## Tips for prompting

- **One step per chat.** Long chats make the AI forget details; start fresh with the master prompt each time.
- **Paste your current code** when asking for changes, and say what happened versus what you expected.
- **Paste the exact error** from the Unity Console (click the error and copy the full text), not a description of it.
- **Ask for small pieces.** "Add sliding to PlayerController.cs" works better than "build the whole player".
- **Keep your data assets** (WeaponData, OperatorData, LevelData) and paste their values back when a prompt needs them.
- **Commit to Git before pasting new code**, so you can roll back if something breaks.
- **Record each step** as a short devlog clip. It doubles as content for your channels.
