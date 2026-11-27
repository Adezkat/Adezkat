# Ashen Anvil: Legends (Unity 3D)

A playable 3D action RPG for Android, built in **Unity 2022.3 LTS** with **C#**, the **Universal Render Pipeline**, **Unity Animator**, **Unity Physics** and **Unity UI**. It uses the heroes and setting of the web version of Ashen Anvil.

You pick a hero at camp and fight through five campaign stages, each with monster waves and a boss. You collect physical loot, level up, equip and forge gear, and claim quest rewards.

## Opening the project

1. Install **Unity 2022.3 LTS** with the **Android Build Support** module (OpenJDK + SDK/NDK) from Unity Hub.
2. In Unity Hub choose **Add > Add project from disk** and pick this `AshenAnvil3D` folder.
3. On the first open, Unity installs URP from `Packages/manifest.json`. The **project generator then runs automatically** (`Ashen Anvil > Rebuild Project Assets`) and creates:
   - the mobile URP pipeline asset, the physics layers and the Android player settings
   - URP materials, animation clips and the shared **Animator Controller**
   - prefabs for every hero, monster, weapon and prop (from the bundled OBJ models)
   - the `MainMenu` and `Game` scenes, registered in Build Settings
4. Open `Assets/AshenAnvil/Scenes/MainMenu.unity` and press **Play**.

You can re-run the generator at any time from the **Ashen Anvil** menu. It is idempotent.

## Building the Android APK

- **In the editor:** `Ashen Anvil > Build Android APK` writes `Builds/Android/AshenAnvilLegends.apk`. `Build Android App Bundle (AAB)` makes a Play Store bundle.
- **From the command line / CI:**
  ```
  Unity -batchmode -quit -projectPath AshenAnvil3D \
        -executeMethod AshenAnvil.EditorTools.AndroidBuilder.BuildApkCommandLine
  ```
  Add `-aab` for an app bundle and `-development` for a development build.

Android settings applied: package `com.ashenanvil.legends`, IL2CPP, ARM64 + ARMv7, min API 24, landscape, Vulkan + GLES3, Linear colour space.

## Controls

| Action | Touch | Keyboard (editor) |
|---|---|---|
| Move | Floating joystick (left half) | WASD / arrows |
| Camera | Drag on the right half | Hold right mouse |
| Attack | Hold **ATTACK** (auto-aims) | J |
| Hero skill | Skill button (cooldown ring) | K |
| Dodge roll (invulnerable) | **DODGE** | Space |
| Potion | **POTION** (stack count shown) | Q |
| Pause | II | Esc |

## Game content

All content is ScriptableObject data under `Assets/AshenAnvil/Data`, registered in `Resources/GameDatabase.asset`.

| Data | Count | Notes |
|---|---|---|
| Heroes (`HeroData`) | 8 | 5 classes (Warrior, Archer, Mage, Assassin, Support), 5 rarities, recruited with gold |
| Skills (`SkillData`) | 8 | Ground Slam, Whirlwind, Fireball, Arrow Volley, Shadow Strike, Healing Nova |
| Weapons (`WeaponData`) | 17 | Sword, Axe, Dagger, Staff, Bow, Hammer, with bonus stats |
| Equipment (`EquipmentData`) | 12 | Helmet, Armor, Boots, Ring, Amulet |
| Consumables (`ConsumableData`) | 4 | Heals plus attack and speed buffs |
| Monsters (`MonsterData`) | 12 | Melee, Ranged (kiting), Charger (wind-up dash) and 5 Bosses (telegraphed slam, summons, enrage) |
| Stages (`StageData`) | 5 | 3 waves + boss each, with their own fog, light and ground palette |
| Quests (`QuestData`) | 8 | Kill counts, bosses, campaign progress, hero level, forging, gold |

## Systems

- **Combat**: crits, defense mitigation, lifesteal, knockback, stuns, area damage with arcs, pooled sphere-cast projectiles with optional explosions and piercing, and invulnerable dodge frames.
- **Unity Physics**: enemies are Rigidbody-driven (real knockback and collisions). Crates and barrels can be pushed and shatter into debris with explosion forces. Loot bounces out with impulses before it is pulled to the player. Collisions are filtered with a layer matrix.
- **Animator**: one controller for every character (Locomotion blend tree, Attack with speed parameter, Cast, Hit, Die). The clips are keyframed on a shared modular rig, so heroes and monsters of any size reuse them.
- **Progression**: hero XP and levels (to 60), stat growth, a gear forge (+15), selling, quests, and first-clear rewards. Progress is saved as JSON in `persistentDataPath` with an atomic write and a backup.
- **UI** (Unity UI / uGUI): camp menu (Campaign, Heroes, Inventory & Forge, Quests, Settings) and a battle HUD with a floating joystick, cooldown rings, boss bar, damage numbers, safe-area support, and pause and results screens.
- **Mobile performance**: SRP Batcher and GPU instancing on shared materials, pooled projectiles, VFX and texts, a single overlay canvas for health bars, static batching for scenery, quality tiers (render scale, shadows, MSAA), a 30/60 FPS cap and no HDR.
- **Audio**: sound effects are synthesised at runtime, so the project needs no audio files.

## Project layout

```
Assets/AshenAnvil/
  Scripts/
    Data/         ScriptableObject definitions (HeroData, WeaponData, MonsterData, StageData...)
    Core/         PlayerProfile (save/progression), SaveSystem, pools, layers, performance
    Combat/       Health, damage, projectiles, VFX
    Characters/   Modular character builder + Animator facade
    Player/       Input, controller, combat & skills, spawner
    Enemies/      EnemyAI state machine
    Items/        Loot tables and physical pickups
    World/        Stage controller, wave manager, destructibles, spawn points
    CameraSystem/ Third-person mobile camera
    UI/           UI toolkit, joystick, buttons, HUD, main menu
    Audio/        Procedural sound effects
  Editor/         Project generator, URP/Android config, animation & prefab & scene builders, APK builder
  Data/           ScriptableObject assets
  Models/         Low-poly OBJ models (weapons, props, scenery)
  Resources/      GameDatabase.asset
  Generated/      (created by the generator) materials, animations, prefabs
  Scenes/         (created by the generator) MainMenu, Game
Tools/            Python generator for data assets, .meta GUIDs and OBJ models
```

## Editing content

- **Balance or new items:** edit the assets in the Inspector, or change `Tools/generate_unity_assets.py` and re-run `python3 Tools/generate_unity_assets.py`. Script GUIDs stay stable.
- **A new hero or monster:** create the asset (`Create > Ashen Anvil > ...`), add it to `GameDatabase`, then run `Ashen Anvil > Rebuild Project Assets` to bake its prefab.
- **Reset save data:** `Ashen Anvil > Delete Save Data`, or *Reset progress* in the in-game Settings.

## Unity version notes

The project targets **2022.3 LTS** (URP 14). It also opens in Unity 6, which upgrades URP automatically. The scripts use `#if UNITY_6000_0_OR_NEWER` for the Rigidbody velocity API change.
