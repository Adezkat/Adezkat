/* =========================================================
   ASHEN ANVIL — static game data
   ========================================================= */
'use strict';

const RARITIES = [
  { key: 'common',    name: 'Common',    color: '#a9b4c2', dark: '#3b4250', mult: 1.00, unlock: 20,  dupe: 10 },
  { key: 'rare',      name: 'Rare',      color: '#3fa2ff', dark: '#123a66', mult: 1.25, unlock: 30,  dupe: 15 },
  { key: 'epic',      name: 'Epic',      color: '#b766ff', dark: '#43166e', mult: 1.60, unlock: 50,  dupe: 25 },
  { key: 'legendary', name: 'Legendary', color: '#ffb526', dark: '#6b4200', mult: 2.10, unlock: 80,  dupe: 40 },
  { key: 'mythic',    name: 'Mythic',    color: '#ff3d5e', dark: '#6e0b1f', mult: 2.80, unlock: 120, dupe: 60 },
];

const CLASSES = {
  warrior:  { name: 'Warrior',  icon: '⚔️', color: '#e0623a', desc: 'Front-line bruiser. High health and defense; strikes the enemy front row.',
              base: { hp: 1300, atk: 80,  def: 60, spd: 90,  crit: 5,  critDmg: 50 } },
  assassin: { name: 'Assassin', icon: '🗡️', color: '#9b5cff', desc: 'Slips past defenders to strike the enemy back row. High crit.',
              base: { hp: 800,  atk: 125, def: 30, spd: 125, crit: 20, critDmg: 70 } },
  mage:     { name: 'Mage',     icon: '🔮', color: '#3fb6ff', desc: 'Wields devastating area magic that hits every enemy.',
              base: { hp: 760,  atk: 135, def: 28, spd: 95,  crit: 8,  critDmg: 50 } },
  archer:   { name: 'Archer',   icon: '🏹', color: '#5fd06a', desc: 'Picks off the weakest foe with rapid volleys.',
              base: { hp: 860,  atk: 112, def: 32, spd: 112, crit: 15, critDmg: 60 } },
  support:  { name: 'Support',  icon: '✨', color: '#ffd24a', desc: 'Heals, shields and empowers allies.',
              base: { hp: 950,  atk: 75,  def: 45, spd: 105, crit: 5,  critDmg: 50 } },
};

/* Skill types:
   strike  – single front target         aoe   – all enemies
   multi   – N hits on random enemies    snipe – lowest-HP enemy
   back    – back-row priority target    heal  – heal lowest ally
   healAll – heal all allies             shield– shield all allies
   Extras: stun{chance,dur}, burn (x atk per sec, 3s), lifesteal, defDown, buffAtk{pct,dur}, shieldAll */
const HEROES = [
  // ---- COMMON
  { id: 'torvald', name: 'Torvald Ironjaw', title: 'Shieldbreaker of Hollow Pass', cls: 'warrior', rarity: 0,
    pal: { main: '#6d7a8c', acc: '#c9d3e0', skin: '#e0b08c', hair: '#8a5a2b', eye: '#7fd4ff' },
    skill: { name: 'Shield Bash', type: 'strike', mult: 1.9, stun: { chance: 0.6, dur: 1.0 }, desc: 'Bashes the front enemy for 190% ATK with a 60% chance to stun for 1s.' },
    awaken: { stat: 'hpPct', val: 15, desc: '+15% HP' },
    lore: 'A mountain sentry who held Hollow Pass alone for three nights. His jaw is iron; his patience is not.' },
  { id: 'nessa', name: 'Nessa Fen', title: 'Marsh Warden', cls: 'archer', rarity: 0,
    pal: { main: '#4f6b3a', acc: '#a3c46b', skin: '#d9a47c', hair: '#3b2a1a', eye: '#b6ff7a' },
    skill: { name: 'Twin Shot', type: 'multi', hits: 2, mult: 1.15, desc: 'Fires 2 arrows at random enemies, each dealing 115% ATK.' },
    awaken: { stat: 'crit', val: 8, desc: '+8% Crit Chance' },
    lore: 'Raised among the reed-folk of the Drowned Fen, Nessa can hear a heron land from a mile away.' },
  { id: 'wick', name: 'Wick the Kindler', title: 'Apprentice Pyromancer', cls: 'mage', rarity: 0,
    pal: { main: '#8c3a1f', acc: '#ff9a3d', skin: '#e8b892', hair: '#ff6a00', eye: '#ffcf4a' },
    skill: { name: 'Ember Burst', type: 'aoe', mult: 0.75, burn: 0.25, desc: 'Blasts all enemies for 75% ATK and burns them for 25% ATK per second for 3s.' },
    awaken: { stat: 'atkPct', val: 12, desc: '+12% ATK' },
    lore: 'Expelled from the Cinder Academy for setting the headmaster\'s beard alight. Twice.' },
  { id: 'morrow', name: 'Morrow Dusk', title: 'Alley Knife', cls: 'assassin', rarity: 0,
    pal: { main: '#2d2a3d', acc: '#8a7cff', skin: '#c99a7a', hair: '#15131f', eye: '#c3b8ff' },
    skill: { name: 'Throat Slit', type: 'back', mult: 2.2, desc: 'Strikes a back-row enemy for 220% ATK.' },
    awaken: { stat: 'critDmg', val: 30, desc: '+30% Crit Damage' },
    lore: 'Nobody in Gallowmere knows Morrow\'s real face. Those who did are no longer in Gallowmere.' },
  { id: 'hilde', name: 'Hilde the Mender', title: 'Field Surgeon', cls: 'support', rarity: 0,
    pal: { main: '#e6e0cf', acc: '#d64545', skin: '#f0c4a0', hair: '#c8a061', eye: '#8fe3ff' },
    skill: { name: 'Mend Wounds', type: 'heal', mult: 2.4, desc: 'Heals the most wounded ally for 240% ATK.' },
    awaken: { stat: 'hpPct', val: 15, desc: '+15% HP' },
    lore: 'She has stitched kings and beggars alike, and charges both the same: one good story.' },
  // ---- RARE
  { id: 'garran', name: 'Garran Oakshield', title: 'Warden of the Old Grove', cls: 'warrior', rarity: 1,
    pal: { main: '#5a4126', acc: '#8fc45a', skin: '#c08a60', hair: '#3d2a14', eye: '#a6ff6b' },
    skill: { name: 'Iron Bark', type: 'shield', mult: 1.6, desc: 'Grants all allies a shield equal to 160% ATK.' },
    awaken: { stat: 'defPct', val: 20, desc: '+20% DEF' },
    lore: 'His shield is carved from the heartwood of a tree older than the kingdom. It still grows.' },
  { id: 'lyra', name: 'Lyra Thornshot', title: 'Briar Huntress', cls: 'archer', rarity: 1,
    pal: { main: '#2f5a4a', acc: '#e04f7a', skin: '#e3b593', hair: '#5a1a2a', eye: '#ff8ab0' },
    skill: { name: 'Piercing Thorn', type: 'snipe', mult: 2.8, defDown: true, desc: 'Shoots the weakest enemy for 280% ATK and lowers its DEF by 30% for 4s.' },
    awaken: { stat: 'atkPct', val: 12, desc: '+12% ATK' },
    lore: 'Her arrows are fletched with briar-rose petals. They are the last thing many beasts ever smell.' },
  { id: 'vesk', name: 'Vesk the Hollow', title: 'Faceless Blade', cls: 'assassin', rarity: 1,
    pal: { main: '#1d2733', acc: '#4fe0c8', skin: '#9fb3b8', hair: '#0b0f14', eye: '#4fffe0' },
    skill: { name: 'Shade Step', type: 'back', mult: 2.6, lifesteal: 0.3, desc: 'Teleports to a back-row enemy, dealing 260% ATK and healing for 30% of damage.' },
    awaken: { stat: 'spd', val: 12, desc: '+12 SPD' },
    lore: 'Vesk traded his name to a shadow for the ability to walk through it.' },
  { id: 'orin', name: 'Orin Sparkwright', title: 'Storm Tinkerer', cls: 'mage', rarity: 1,
    pal: { main: '#2b3d73', acc: '#6fd8ff', skin: '#e0b48a', hair: '#e8e8e8', eye: '#9ff2ff' },
    skill: { name: 'Chain Lightning', type: 'multi', hits: 4, mult: 0.85, stun: { chance: 0.15, dur: 0.8 }, desc: 'Lightning jumps 4 times for 85% ATK each, 15% chance to stun.' },
    awaken: { stat: 'atkPct', val: 12, desc: '+12% ATK' },
    lore: 'Half wizard, half engineer, wholly unwelcome near anything flammable.' },
  { id: 'calla', name: 'Sister Calla', title: 'Lantern of the Abbey', cls: 'support', rarity: 1,
    pal: { main: '#3b3f6b', acc: '#ffe08a', skin: '#f0caa8', hair: '#f5e6c4', eye: '#fff2a8' },
    skill: { name: 'Prayer of Light', type: 'healAll', mult: 1.1, desc: 'Heals all allies for 110% ATK.' },
    awaken: { stat: 'hpPct', val: 15, desc: '+15% HP' },
    lore: 'Her lantern has not gone out in forty years. She claims she has never refilled it.' },
  // ---- EPIC
  { id: 'kael', name: 'Kael Bloodfang', title: 'Wolf of the Red Steppe', cls: 'warrior', rarity: 2,
    pal: { main: '#5c1a1a', acc: '#e23b3b', skin: '#c9906a', hair: '#1a1a1a', eye: '#ff4040' },
    skill: { name: 'Blood Frenzy', type: 'aoe', mult: 1.25, lifesteal: 0.35, desc: 'Cleaves all enemies for 125% ATK, healing for 35% of damage dealt.' },
    awaken: { stat: 'atkPct', val: 15, desc: '+15% ATK' },
    lore: 'Raised by the dire wolves of the steppe, Kael still howls before battle. So does his axe.' },
  { id: 'ysolde', name: 'Ysolde Frostveil', title: 'Winter\'s Daughter', cls: 'mage', rarity: 2,
    pal: { main: '#cfe8ff', acc: '#5ac8ff', skin: '#f2dcd0', hair: '#eaf6ff', eye: '#6ae2ff' },
    skill: { name: 'Glacial Nova', type: 'aoe', mult: 1.1, stun: { chance: 0.45, dur: 1.2 }, desc: 'Freezes all enemies for 110% ATK with a 45% chance to stun for 1.2s.' },
    awaken: { stat: 'hpPct', val: 18, desc: '+18% HP' },
    lore: 'Born during the Hundred-Day Winter. Snow still falls wherever she weeps.' },
  { id: 'sable', name: 'Sable Nightwhisper', title: 'Mistress of Knives', cls: 'assassin', rarity: 2,
    pal: { main: '#1e1028', acc: '#d24bff', skin: '#d8b2a0', hair: '#4a1060', eye: '#ff7bff' },
    skill: { name: 'Thousand Cuts', type: 'multi', hits: 6, mult: 0.7, desc: 'A flurry of 6 strikes on random enemies, each for 70% ATK.' },
    awaken: { stat: 'crit', val: 10, desc: '+10% Crit Chance' },
    lore: 'She speaks only in whispers, because the knives are listening.' },
  { id: 'aurek', name: 'Brother Aurek', title: 'Warhymn Cantor', cls: 'support', rarity: 2,
    pal: { main: '#6b4a1a', acc: '#ffcf4a', skin: '#c48a5e', hair: '#2a1a0a', eye: '#ffe680' },
    skill: { name: 'Warhymn', type: 'healAll', mult: 0.8, buffAtk: { pct: 30, dur: 5 }, desc: 'Heals all allies for 80% ATK and raises their ATK by 30% for 5s.' },
    awaken: { stat: 'spd', val: 12, desc: '+12 SPD' },
    lore: 'His hymns once turned a rout into a victory. The enemy general still hums them in his sleep.' },
  { id: 'fenna', name: 'Fenna Galeshot', title: 'Skyreach Ranger', cls: 'archer', rarity: 2,
    pal: { main: '#3a5f7a', acc: '#bff0ff', skin: '#e8c2a0', hair: '#c9e9ff', eye: '#d0faff' },
    skill: { name: 'Arrow Storm', type: 'aoe', mult: 1.05, desc: 'Rains arrows on all enemies for 105% ATK.' },
    awaken: { stat: 'atkPct', val: 15, desc: '+15% ATK' },
    lore: 'She rides the updrafts of the Skyreach cliffs, loosing arrows on the wind itself.' },
  // ---- LEGENDARY
  { id: 'draven', name: 'Draven Emberheart', title: 'The Burning Vow', cls: 'warrior', rarity: 3,
    pal: { main: '#3a1208', acc: '#ff7a1a', skin: '#d49a72', hair: '#ff4a00', eye: '#ffb000' },
    skill: { name: 'Inferno Cleave', type: 'aoe', mult: 1.7, burn: 0.35, desc: 'Cleaves all enemies for 170% ATK and burns them for 35% ATK per second.' },
    awaken: { stat: 'atkPct', val: 20, desc: '+20% ATK' },
    lore: 'He swore an oath upon a dying star. Now its fire beats in his chest instead of a heart.' },
  { id: 'nyx', name: 'Nyx Starfall', title: 'Oracle of the Shattered Sky', cls: 'mage', rarity: 3,
    pal: { main: '#140e33', acc: '#9b8cff', skin: '#c8c0e8', hair: '#d8d0ff', eye: '#ffffff' },
    skill: { name: 'Meteor', type: 'aoe', mult: 2.1, desc: 'Calls down a meteor dealing 210% ATK to all enemies.' },
    awaken: { stat: 'critDmg', val: 40, desc: '+40% Crit Damage' },
    lore: 'Nyx reads the future in falling stars — and occasionally helps them fall.' },
  { id: 'rhea', name: 'Rhea Stormfletch', title: 'Thunder\'s Reach', cls: 'archer', rarity: 3,
    pal: { main: '#24304d', acc: '#ffe14a', skin: '#e2b088', hair: '#202a40', eye: '#fff36a' },
    skill: { name: 'Storm Volley', type: 'multi', hits: 6, mult: 0.95, stun: { chance: 0.12, dur: 0.8 }, desc: '6 lightning arrows for 95% ATK each, 12% chance to stun.' },
    awaken: { stat: 'crit', val: 12, desc: '+12% Crit Chance' },
    lore: 'Her bowstring was spun from a captured thunderbolt. It hums before every storm.' },
  { id: 'seraphine', name: 'Seraphine Dawnbrand', title: 'Herald of First Light', cls: 'support', rarity: 3,
    pal: { main: '#f5ecd4', acc: '#ffc23a', skin: '#f5d2b4', hair: '#ffe28a', eye: '#fff6c0' },
    skill: { name: 'Dawn\'s Grace', type: 'healAll', mult: 1.6, shieldAll: 0.8, desc: 'Heals all allies for 160% ATK and shields them for 80% ATK.' },
    awaken: { stat: 'hpPct', val: 22, desc: '+22% HP' },
    lore: 'Where Seraphine walks, the night recedes a step. The undead have learned to fear sunrise.' },
  { id: 'kestrel', name: 'Kestrel Blackveil', title: 'The Final Contract', cls: 'assassin', rarity: 3,
    pal: { main: '#101418', acc: '#ff3a3a', skin: '#b89888', hair: '#e0e0e0', eye: '#ff2a2a' },
    skill: { name: 'Death Mark', type: 'snipe', mult: 3.8, execute: true, desc: 'Strikes the weakest enemy for 380% ATK; deals double damage below 30% HP.' },
    awaken: { stat: 'critDmg', val: 40, desc: '+40% Crit Damage' },
    lore: 'Every contract Kestrel has accepted has been fulfilled. Including the one on her own life.' },
  // ---- MYTHIC
  { id: 'oberon', name: 'Oberon', title: 'The Ashen King', cls: 'warrior', rarity: 4,
    pal: { main: '#2a2420', acc: '#ff5a1f', skin: '#a8a0a0', hair: '#4a4040', eye: '#ff8a2a' },
    skill: { name: 'Cinder Throne', type: 'aoe', mult: 2.0, stun: { chance: 0.6, dur: 1.5 }, burn: 0.3, desc: 'Erupts for 200% ATK to all enemies, 60% chance to stun for 1.5s, and burns them.' },
    awaken: { stat: 'hpPct', val: 25, desc: '+25% HP' },
    lore: 'The last king of a kingdom that burned. He rules its ashes still, and the ashes obey.' },
  { id: 'azhral', name: 'Azhral the Unbound', title: 'Devourer of Names', cls: 'mage', rarity: 4,
    pal: { main: '#1a0628', acc: '#c03aff', skin: '#8a7a9a', hair: '#0a0010', eye: '#e070ff' },
    skill: { name: 'Void Cataclysm', type: 'aoe', mult: 2.7, defDown: true, desc: 'Tears open the void for 270% ATK to all enemies and lowers their DEF by 30%.' },
    awaken: { stat: 'atkPct', val: 25, desc: '+25% ATK' },
    lore: 'Sealed for a thousand years beneath the Obsidian Library. The seal was a library card. It expired.' },
  { id: 'valka', name: 'Valka Soulreaver', title: 'Moon-Cursed Huntress', cls: 'assassin', rarity: 4,
    pal: { main: '#2a0a12', acc: '#ff2a4a', skin: '#d0b0b0', hair: '#f0f0f0', eye: '#ff2040' },
    skill: { name: 'Reap', type: 'back', mult: 4.8, lifesteal: 0.5, desc: 'Reaps a back-row enemy for 480% ATK, healing for 50% of damage.' },
    awaken: { stat: 'critDmg', val: 50, desc: '+50% Crit Damage' },
    lore: 'Cursed beneath a blood moon, Valka hunts the things that hunt in the dark.' },
  { id: 'lumen', name: 'Lumen Eternal', title: 'The Undying Flame', cls: 'support', rarity: 4,
    pal: { main: '#fff6e0', acc: '#7ae8ff', skin: '#fff0e0', hair: '#bff6ff', eye: '#ffffff' },
    skill: { name: 'Eternal Light', type: 'healAll', mult: 2.2, buffAtk: { pct: 40, dur: 6 }, desc: 'Heals all allies for 220% ATK and increases their ATK by 40% for 6s.' },
    awaken: { stat: 'hpPct', val: 25, desc: '+25% HP' },
    lore: 'A flame that refused to die when the first sun went out. It has been looking for a new sky.' },
  { id: 'sylvan', name: 'Sylvan Wyrdbow', title: 'Archer of the World Tree', cls: 'archer', rarity: 4,
    pal: { main: '#1a3a1a', acc: '#7aff5a', skin: '#c8b890', hair: '#2a5a1a', eye: '#aaff6a' },
    skill: { name: 'Wyrd Arrows', type: 'multi', hits: 8, mult: 0.95, burn: 0.2, desc: '8 enchanted arrows for 95% ATK each that set foes aflame.' },
    awaken: { stat: 'atkPct', val: 25, desc: '+25% ATK' },
    lore: 'Sylvan\'s bow is a living branch of the World Tree. Each arrow it fires grows back by dawn.' },
];
const HERO_BY_ID = Object.fromEntries(HEROES.map(h => [h.id, h]));

/* ---------- Monsters ---------- */
const MONSTER_KINDS = {
  slime:    { name: 'Gloom Slime',   ranged: false, hp: 1.0,  atk: 0.8, def: 0.8, spd: 85,  col: '#5ad06a' },
  goblin:   { name: 'Rot Goblin',    ranged: false, hp: 0.8,  atk: 1.0, def: 0.7, spd: 115, col: '#8aa640' },
  wolf:     { name: 'Dire Wolf',     ranged: false, hp: 0.9,  atk: 1.1, def: 0.7, spd: 120, col: '#7a7a8a' },
  skeleton: { name: 'Bone Archer',   ranged: true,  hp: 0.75, atk: 1.15,def: 0.6, spd: 100, col: '#e8e2cc' },
  orc:      { name: 'Ash Orc',       ranged: false, hp: 1.3,  atk: 1.0, def: 1.1, spd: 90,  col: '#5a8a4a' },
  spider:   { name: 'Venom Spider',  ranged: false, hp: 0.85, atk: 1.1, def: 0.8, spd: 125, col: '#5a3a6a' },
  wraith:   { name: 'Hollow Wraith', ranged: true,  hp: 0.7,  atk: 1.25,def: 0.5, spd: 110, col: '#7ad0ff' },
  golem:    { name: 'Rune Golem',    ranged: false, hp: 1.6,  atk: 0.8, def: 1.5, spd: 75,  col: '#8a7a6a' },
  demon:    { name: 'Pit Fiend',     ranged: true,  hp: 1.0,  atk: 1.2, def: 0.9, spd: 105, col: '#d0402a' },
  dragon:   { name: 'Drakeling',     ranged: true,  hp: 1.2,  atk: 1.15,def: 1.0, spd: 100, col: '#c03a3a' },
};
const MONSTER_KEYS = Object.keys(MONSTER_KINDS);
const MONSTER_BASE = { hp: 900, atk: 78, def: 38, crit: 5, critDmg: 50 };

const CHAPTERS = [
  'Ashfall Outskirts', 'The Drowned Fen', 'Hollow Pass', 'Gallowmere', 'Cinder Woods',
  'Bonefield Crossing', 'The Weeping Mines', 'Frostgrave Peaks', 'Thornspire Ruins', 'Blackwater Keep',
  'The Obsidian Library', 'Red Steppe', 'Skyreach Cliffs', 'Moonless Forest', 'Ironmaw Citadel',
  'Gloamtide Coast', 'The Sunken Cathedral', 'Ember Wastes', 'Shrieking Hollows', 'Tomb of Kings',
  'Starfall Crater', 'The Veil Marches', 'Abyssal Gate', 'Wyrmspine Ridge', 'Throne of Cinders',
  'The Shattered Sky', 'Void Rift', 'World Tree Roots', 'Hall of the Undying', 'The Ashen Crown',
];
const STAGES_PER_CHAPTER = 20;
const MAX_STAGE = CHAPTERS.length * STAGES_PER_CHAPTER; // 600

const BOSS_NAMES = [
  'Gorrak the Rotten', 'Mirewyrm Hag', 'Pass Tyrant Ulmog', 'The Hanged Judge', 'Charbark Ancient',
  'Bone Colossus', 'Deepdelver Matriarch', 'Rimeclaw Yeti', 'Thornspire Seneschal', 'Blackwater Revenant',
  'The Ink Devourer', 'Khan Ruuvak', 'Storm Roc Ascendant', 'Moonless Stalker', 'Ironmaw Warlord',
  'Tidecaller Siren', 'Drowned Bishop', 'Ember Djinn', 'The Shrieker', 'The Tomb King',
  'Starborn Horror', 'Veil Weaver', 'Abyssal Gatekeeper', 'Wyrmspine Matriarch', 'Cinder Regent',
  'Skyshatter Titan', 'The Rift Maw', 'Rootrot Behemoth', 'The Undying Choir', 'The Ashen Crown',
];

/* ---------- Equipment ---------- */
const SLOTS = ['weapon', 'helmet', 'armor', 'boots', 'accessory'];
const SLOT_INFO = {
  weapon:    { name: 'Weapon',    icon: '⚔️', bases: ['Blade', 'Greatsword', 'Warhammer', 'Runeblade', 'Waraxe', 'Glaive'] },
  helmet:    { name: 'Helmet',    icon: '⛑️', bases: ['Helm', 'Crown', 'Visor', 'Hood', 'Greathelm'] },
  armor:     { name: 'Armor',     icon: '🛡️', bases: ['Cuirass', 'Hauberk', 'Plate', 'Robes', 'Brigandine'] },
  boots:     { name: 'Boots',     icon: '👢', bases: ['Boots', 'Greaves', 'Treads', 'Sabatons'] },
  accessory: { name: 'Accessory', icon: '💍', bases: ['Ring', 'Amulet', 'Talisman', 'Sigil', 'Charm'] },
};
const RARITY_PREFIX = [
  ['Iron', 'Worn', 'Rusted', 'Plain'],
  ['Steel', 'Tempered', 'Honed', 'Sturdy'],
  ['Runic', 'Shadowforged', 'Moonlit', 'Gilded'],
  ['Dragonbone', 'Sunfire', 'Stormcaller', 'Kingslayer'],
  ['Voidborn', 'Celestial', 'Ashen Crown', 'Worldbreaker'],
];
const STAT_NAMES = {
  hp: 'HP', atk: 'ATK', def: 'DEF', spd: 'SPD', crit: 'Crit Chance', critDmg: 'Crit DMG',
  hpPct: 'HP', atkPct: 'ATK', defPct: 'DEF',
};
const PCT_STATS = new Set(['hpPct', 'atkPct', 'defPct', 'crit', 'critDmg']);
const AFFIX_POOL = [
  { stat: 'atkPct', min: 2, max: 6 },
  { stat: 'hpPct', min: 2, max: 6 },
  { stat: 'defPct', min: 3, max: 8 },
  { stat: 'crit', min: 1, max: 4 },
  { stat: 'critDmg', min: 4, max: 12 },
  { stat: 'spd', min: 1, max: 5 },
  { stat: 'atk', min: 6, max: 14, scaled: true },
  { stat: 'hp', min: 60, max: 140, scaled: true },
];
const SPECIALS = {
  lifesteal: { name: 'Vampiric',   desc: 'Heal for 12% of damage dealt.' },
  thorns:    { name: 'Thornmail',  desc: 'Reflect 20% of damage taken.' },
  burn:      { name: 'Searing',    desc: '25% chance on hit to burn for 30% ATK/s for 3s.' },
  stun:      { name: 'Concussive', desc: '10% chance on hit to stun for 1.2s.' },
  execute:   { name: 'Executioner',desc: '+60% damage against enemies below 30% HP.' },
  bulwark:   { name: 'Bulwark',    desc: 'Start each battle with a shield of 25% max HP.' },
  frenzy:    { name: 'Frenzied',   desc: '+4% ATK each attack, stacking up to 10 times.' },
  regen:     { name: 'Troll Blood',desc: 'Regenerate 2% max HP per second.' },
  focus:     { name: 'Arcane Focus', desc: '+35% energy gain; skills come faster.' },
  evasion:   { name: 'Phantom',    desc: '12% chance to dodge attacks.' },
};
const SPECIAL_KEYS = Object.keys(SPECIALS);

/* ---------- Artifacts ---------- */
const ARTIFACTS = [
  { id: 'ember_crown',   name: 'Ember Crown',       icon: '👑', stat: 'atkPct', per: 3, desc: 'Team ATK', col: '#ff8a2a' },
  { id: 'ymir_heart',    name: 'Heart of Ymir',     icon: '💠', stat: 'hpPct',  per: 3, desc: 'Team HP', col: '#6ad0ff' },
  { id: 'aegis_shard',   name: 'Aegis Shard',       icon: '🔰', stat: 'defPct', per: 4, desc: 'Team DEF', col: '#ffd24a' },
  { id: 'raven_eye',     name: 'Raven\'s Eye',      icon: '👁️', stat: 'crit',   per: 1, desc: 'Team Crit Chance', col: '#b766ff' },
  { id: 'wyrm_tooth',    name: 'Wyrm Tooth',        icon: '🦷', stat: 'critDmg',per: 5, desc: 'Team Crit DMG', col: '#ff3d5e' },
  { id: 'hermes_quill',  name: 'Gale Quill',        icon: '🪶', stat: 'spd',    per: 2, desc: 'Team SPD', col: '#7affc8' },
  { id: 'gilded_ledger', name: 'Gilded Ledger',     icon: '📒', stat: 'goldPct',per: 5, desc: 'Gold from all sources', col: '#ffcf4a' },
  { id: 'sage_lamp',     name: 'Sage\'s Lamp',      icon: '🪔', stat: 'essPct', per: 5, desc: 'Essence from all sources', col: '#7ae8ff' },
  { id: 'forge_soul',    name: 'Soul of the Forge', icon: '🔥', stat: 'forgeLuck', per: 1, desc: 'Higher forge rarity odds', col: '#ff5a1f' },
  { id: 'hourglass',     name: 'Sleeper\'s Hourglass', icon: '⏳', stat: 'afkPct', per: 4, desc: 'AFK reward rate', col: '#c9b48a' },
  { id: 'moon_sigil',    name: 'Blood Moon Sigil',  icon: '🌑', stat: 'atkPct', per: 4, desc: 'Team ATK', col: '#ff2a4a' },
  { id: 'lumen_tear',    name: 'Tear of Lumen',     icon: '💧', stat: 'hpPct',  per: 4, desc: 'Team HP', col: '#bff6ff' },
];
const ART_BY_ID = Object.fromEntries(ARTIFACTS.map(a => [a.id, a]));
const ARTIFACT_MAX = 10;

/* ---------- Resources ---------- */
const RES = {
  gold:       { name: 'Gold',            icon: '🪙', desc: 'Universal currency. Earned from battles and AFK.' },
  gems:       { name: 'Gems',            icon: '💎', desc: 'Premium currency for summons and refills.' },
  ore:        { name: 'Ember Ore',       icon: '🪨', desc: 'Fuel for the Forge. 1 ore = 1 forged item.' },
  steel:      { name: 'Starsteel',       icon: '🔩', desc: 'Used to enhance equipment.' },
  essence:    { name: 'Spirit Essence',  icon: '🌀', desc: 'Used to level up heroes.' },
  tomes:      { name: 'Skill Tomes',     icon: '📘', desc: 'Used to upgrade hero skills.' },
  stones:     { name: 'Awakening Stones',icon: '🔮', desc: 'Used to awaken max-star heroes.' },
  scrolls:    { name: 'Summon Scrolls',  icon: '📜', desc: 'Summon a hero at the Tavern.' },
  tickets:    { name: 'Arena Tickets',   icon: '🎟️', desc: 'Spent to challenge Arena opponents.' },
  arenaCoins: { name: 'Glory Marks',     icon: '🏅', desc: 'Arena currency. Spend in the Arena shop.' },
  guildCoins: { name: 'Guild Seals',     icon: '🛡️', desc: 'Guild currency. Spend in the Guild shop.' },
  moonTokens: { name: 'Moon Tokens',     icon: '🌕', desc: 'Event currency for the Blood Moon Siege.' },
  artFrags:   { name: 'Relic Dust',      icon: '✨', desc: 'Converts into fragments of any artifact.' },
};
const CONSUMABLES = {
  goldBag:     { name: 'Gold Pouch',       icon: '💰', desc: 'Open to receive 2 hours of AFK gold.' },
  essFlask:    { name: 'Essence Flask',    icon: '🧪', desc: 'Open to receive 2 hours of AFK essence.' },
  oreCrate:    { name: 'Ore Crate',        icon: '📦', desc: 'Contains 25 Ember Ore.' },
  mysteryChest:{ name: 'Mystery Chest',    icon: '🎁', desc: 'Contains a random assortment of riches.' },
  heroChest:   { name: 'Hero Shard Chest', icon: '🧰', desc: 'Contains 15 shards of a random Epic+ hero.' },
};

/* ---------- Missions & achievements ---------- */
const DAILY_MISSIONS = [
  { key: 'win',     name: 'Win 3 campaign battles',   goal: 3, pts: 20 },
  { key: 'forge',   name: 'Forge 5 items',            goal: 5, pts: 20 },
  { key: 'enhance', name: 'Enhance equipment 3 times',goal: 3, pts: 15 },
  { key: 'arena',   name: 'Fight 2 Arena battles',    goal: 2, pts: 15 },
  { key: 'afk',     name: 'Collect AFK rewards 2 times', goal: 2, pts: 10 },
  { key: 'levelup', name: 'Level up heroes 5 times',  goal: 5, pts: 10 },
  { key: 'summon',  name: 'Summon 1 hero',            goal: 1, pts: 10 },
  { key: 'guild',   name: 'Donate or fight in Guild', goal: 1, pts: 10 },
];
const ACTIVITY_CHESTS = [
  { at: 20,  reward: { gold: 5000, essence: 2000 } },
  { at: 40,  reward: { ore: 20, steel: 40 } },
  { at: 60,  reward: { gems: 60, tomes: 3 } },
  { at: 80,  reward: { scrolls: 1, steel: 60 } },
  { at: 100, reward: { gems: 120, stones: 1, artFrags: 5 } },
];
const ACHIEVEMENTS = [
  { key: 'stage',   name: 'Conqueror',      desc: 'Clear campaign stage {n}', tiers: [10, 25, 50, 100, 150, 200, 300, 400, 500, 600] },
  { key: 'forged',  name: 'Master Smith',   desc: 'Forge {n} items',          tiers: [10, 50, 150, 400, 1000, 2500] },
  { key: 'enhanced',name: 'Tempering',      desc: 'Enhance equipment {n} times', tiers: [10, 50, 200, 500, 1500] },
  { key: 'heroes',  name: 'Collector',      desc: 'Own {n} heroes',           tiers: [6, 10, 15, 20, 25] },
  { key: 'summoned',name: 'Tavern Regular', desc: 'Summon {n} heroes',        tiers: [10, 50, 150, 400] },
  { key: 'arenaWins',name: 'Gladiator',     desc: 'Win {n} Arena battles',     tiers: [5, 25, 100, 250, 500] },
  { key: 'bosses',  name: 'Bane of Tyrants',desc: 'Defeat {n} campaign bosses',tiers: [3, 10, 25, 45, 60] },
  { key: 'power',   name: 'Rising Legend',  desc: 'Reach {n} team power',     tiers: [5000, 15000, 40000, 100000, 250000, 600000, 1500000] },
  { key: 'mythic',  name: 'Mythsmith',      desc: 'Forge {n} Mythic items',   tiers: [1, 5, 20] },
];
const LOGIN_REWARDS = [
  { gold: 10000 }, { gems: 100 }, { ore: 30 }, { scrolls: 2 }, { steel: 100, tomes: 5 }, { gems: 200 }, { scrolls: 5, stones: 2 },
];

/* ---------- Guild ---------- */
const GUILD_NAMES = [
  ['Order of the Ashen Rose', '🌹'], ['Iron Wolves', '🐺'], ['Starfall Covenant', '🌠'], ['Bloodmoon Pact', '🌑'],
  ['The Gilded Anvil', '⚒️'], ['Wardens of Dusk', '🦉'], ['Cinderborn', '🔥'], ['Frostveil Sisters', '❄️'],
  ['House Ravenmark', '🐦‍⬛'], ['The Hollow Crown', '👑'],
];
const GUILD_BOSSES = ['Gorath, Molten Wyrm', 'The Rimebound Colossus', 'Mother of Spiders', 'The Abyssal Choir', 'Ashen Leviathan'];

/* ---------- Name generation for simulated players ---------- */
const NAME_A = ['Shadow', 'Iron', 'Grim', 'Night', 'Storm', 'Ash', 'Blood', 'Frost', 'Ember', 'Raven', 'Void', 'Silver', 'Dusk', 'Thorn', 'Wolf', 'Star', 'Bone', 'Moon', 'Sun', 'Rune'];
const NAME_B = ['blade', 'heart', 'fang', 'born', 'walker', 'reaper', 'caller', 'smith', 'bane', 'mourne', 'wing', 'forge', 'shade', 'claw', 'song', 'crest', 'thorn', 'veil', 'hunter', 'sworn'];
