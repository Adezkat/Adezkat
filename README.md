# Ashen Anvil: Idle Legends

An original dark-fantasy **idle RPG** for Android phones (and any mobile browser): hero collection, equipment forging, auto-battles, PvP arena and guilds.

## Play
- **Local:** serve the folder with any static server (`python3 -m http.server`) and open `index.html` on your phone.
- **Install on Android:** host it (e.g. GitHub Pages), open it in Chrome and choose *Add to Home screen*. It runs full-screen and works offline (PWA).
- **Native APK (Capacitor):** the `android/` project is already set up.
  - *Automatic:* every push runs the **Build Android APK** GitHub Action. Download `AshenAnvil-apk` from the run's Artifacts, unzip it and install the `.apk` on your phone (allow installs from unknown sources).
  - *Local:* with Node 22, JDK 21 and the Android SDK installed, run `npm ci && npm run apk:debug`. The APK is written to `android/app/build/outputs/apk/debug/`. Or run `npm run sync && npx cap open android` to open it in Android Studio.
  - After editing the game files, run `npm run sync` to copy them into the Android project.

Progress saves automatically in local storage. Use Settings to export or import a save code.

## Features
| System | What's in it |
|---|---|
| Heroes | 25 original heroes, 5 rarities (Common → Mythic), 5 classes, each with a unique skill. Level up, evolve with shards (up to 7★), awaken (3 tiers), upgrade skills, 5-hero formation with front and back rows |
| Forge | Forge 5 slot types with random affixes. Legendary/Mythic items roll special effects such as lifesteal, thorns, burn, stun, execute and dodge. Upgrade the forge for better odds. Combine 3 items into the next rarity, enhance up to +25, salvage, lock, auto-equip |
| Battle | Real-time automatic combat: speed-based turns, energy-driven skills, crits, shields, burns, stuns, buffs, animated projectiles and damage numbers, ×1/×2/×3 speed and skip |
| Campaign | 600 stages in 30 chapters, with a boss every 10 stages and scaling difficulty |
| Idle | AFK loot for up to 12 hours (including offline), Quick Loot, an idle hunt on the home screen |
| Arena | Rated matches against generated opponent teams, 6 tiers, leaderboard, weekly rewards, arena shop |
| Guild | Join or create a guild, shared-HP guild boss, donations, guild levels with team perks, weekly guild war, guild ranking and shop |
| Progression | 12 artifacts, daily missions with activity chests, 9 achievement lines, 7-day login calendar, weekly events (Blood Moon Siege and Forge Festival) |

## Code layout
`js/data.js` content · `js/state.js` economy and save · `js/battle.js` combat engine and renderer · `js/art.js` procedural SVG art · `js/ui.js` screens · `js/main.js` navigation and actions.
