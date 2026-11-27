/* =========================================================
   Game state, persistence, economy and progression rules
   ========================================================= */
'use strict';

const SAVE_KEY = 'ashen-anvil-save-v1';
const AFK_CAP_H = 12;
const G = { state: null };

G.newState = () => {
  const now = Date.now();
  const s = {
    v: 1, name: 'Wanderer', created: now, lastSeen: now,
    day: U.dayKey(), week: U.weekKey(),
    res: { gold: 8000, gems: 600, ore: 40, steel: 60, essence: 6000, tomes: 5, stones: 0, scrolls: 5, tickets: 5, arenaCoins: 0, guildCoins: 0, moonTokens: 0, artFrags: 0 },
    items: { goldBag: 1, essFlask: 1, oreCrate: 1, mysteryChest: 1, heroChest: 0 },
    heroes: {}, shards: {},
    team: ['torvald', 'hilde', 'morrow', 'wick', 'nessa'],
    gear: [], nextUid: 1,
    forge: { lvl: 1 },
    stage: 1,
    afkSince: now, quickLootFree: 1,
    arena: { rating: 1000, best: 1000, wins: 0, losses: 0, opps: [], refresh: 0, log: [], week: U.weekKey(), pendingReward: null },
    guild: null, guildLeftDay: null,
    artifacts: {},
    daily: { prog: {}, claimed: {}, chests: [] },
    stats: { forged: 0, enhanced: 0, summoned: 0, arenaWins: 0, bosses: 0, mythic: 0, battles: 0 },
    ach: {},
    login: { count: 0, last: '' },
    shop: { day: '', deals: [], bought: {}, freeGems: '', freeGold: '', arenaBought: {}, guildBought: {} },
    events: { week: U.weekKey(), siege: 1, festForged: 0, festClaimed: [], shopBought: {} },
    pity: 0,
    settings: { sound: true, speed: 1, autoPush: false },
  };
  ['torvald', 'hilde', 'morrow', 'wick', 'nessa'].forEach(id => { s.heroes[id] = G.makeHeroInst(id); });
  return s;
};

G.makeHeroInst = id => ({ id, lvl: 1, stars: HERO_BY_ID[id].rarity + 1, awaken: 0, skill: 1, equip: { weapon: null, helmet: null, armor: null, boots: null, accessory: null } });

G.save = () => {
  if (!G.state) return;
  G.state.lastSeen = Date.now();
  try { localStorage.setItem(SAVE_KEY, JSON.stringify(G.state)); } catch (e) { /* storage full or blocked */ }
};
G.load = () => {
  let raw = null;
  try { raw = localStorage.getItem(SAVE_KEY); } catch (e) { raw = null; }
  if (raw) {
    try {
      const loaded = JSON.parse(raw);
      const fresh = G.newState();
      // shallow-merge to tolerate older saves
      for (const k of Object.keys(fresh)) if (loaded[k] === undefined) loaded[k] = fresh[k];
      for (const k of ['res', 'items', 'stats', 'settings', 'arena', 'events', 'shop', 'daily', 'login']) loaded[k] = Object.assign({}, fresh[k], loaded[k]);
      G.state = loaded;
      return true;
    } catch (e) { /* corrupted save */ }
  }
  G.state = G.newState();
  return false;
};
G.reset = () => { try { localStorage.removeItem(SAVE_KEY); } catch (e) { } G.state = G.newState(); G.save(); };
G.exportSave = () => btoa(unescape(encodeURIComponent(JSON.stringify(G.state))));
G.importSave = str => {
  const obj = JSON.parse(decodeURIComponent(escape(atob(str.trim()))));
  if (!obj || !obj.heroes || !obj.res) throw new Error('Invalid save');
  localStorage.setItem(SAVE_KEY, JSON.stringify(obj));
  G.load();
};

/* ---------------- Daily / weekly resets ---------------- */
G.checkResets = () => {
  const s = G.state, today = U.dayKey(), wk = U.weekKey();
  const msgs = [];
  if (s.day !== today) {
    s.day = today;
    s.daily = { prog: {}, claimed: {}, chests: [] };
    s.res.tickets = Math.max(s.res.tickets, 5);
    s.quickLootFree = 1;
    s.shop.deals = []; s.shop.bought = {};
    if (s.guild) {
      const gd = s.guild;
      gd.attempts = 3; gd.donations = 0; gd.warAttacks = 2;
      // simulated guildmates' daily contribution
      gd.xp += gd.members.length * 35;
      G.guildLevelCheck();
      G.guildBossDamage(gd.bossMax * (0.04 + gd.members.length * 0.006), false);
      gd.members.forEach(m => { m.contrib += U.randi(20, 120); });
    }
    msgs.push('daily');
  }
  if (s.arena.week !== wk) {
    const tier = G.arenaTier(s.arena.rating);
    s.arena.pendingReward = { tier: tier.name, gems: tier.gems, arenaCoins: tier.coins, scrolls: tier.scrolls };
    s.arena.week = wk;
    s.arena.rating = Math.round(1000 + (s.arena.rating - 1000) * 0.7);
    s.arena.opps = [];
    s.shop.arenaBought = {}; s.shop.guildBought = {};
    if (s.guild) {
      const w = s.guild.war;
      if (w) {
        const ours = w.myStars + w.allyStars;
        s.guild.pendingWar = { win: ours >= w.enemyStars, ours, theirs: w.enemyStars, enemy: w.enemy };
      }
      s.guild.war = null;
    }
    msgs.push('weekly');
  }
  if (s.events.week !== wk) {
    s.events = { week: wk, siege: 1, festForged: 0, festClaimed: [], shopBought: {} };
  }
  return msgs;
};

/* ---------------- Resources ---------------- */
G.can = cost => Object.entries(cost).every(([k, v]) => (G.state.res[k] ?? G.state.items[k] ?? 0) >= v);
G.spend = cost => {
  if (!G.can(cost)) return false;
  for (const [k, v] of Object.entries(cost)) { if (k in G.state.res) G.state.res[k] -= v; else G.state.items[k] -= v; }
  return true;
};
G.missing = cost => Object.entries(cost).filter(([k, v]) => (G.state.res[k] ?? 0) < v).map(([k]) => RES[k] ? RES[k].name : k).join(', ');
G.costLabel = cost => Object.entries(cost).map(([k, v]) => {
  const have = G.state.res[k] ?? 0;
  return `<span class="${have >= v ? '' : 'bad'}">${RES[k].icon} ${U.fmt(v)}</span>`;
}).join(' ');
G.grant = rew => {
  const s = G.state;
  for (const [k, v] of Object.entries(rew)) {
    if (!v) continue;
    if (k in s.res) s.res[k] += v;
    else if (k in CONSUMABLES) s.items[k] = (s.items[k] || 0) + v;
    else if (k.startsWith('shard:')) { const id = k.slice(6); s.shards[id] = (s.shards[id] || 0) + v; }
    else if (k.startsWith('art:')) G.addArtFrags(k.slice(4), v);
  }
};

/* ---------------- Bonuses ---------------- */
G.artLevel = id => (G.state.artifacts[id] && G.state.artifacts[id].lvl) || 0;
G.artBonus = stat => ARTIFACTS.reduce((s, a) => s + (a.stat === stat ? a.per * G.artLevel(a.id) : 0), 0);
G.guildBonus = () => G.state.guild ? G.state.guild.lvl : 0; // % atk & hp
G.goldMult = () => 1 + G.artBonus('goldPct') / 100;
G.essMult = () => 1 + G.artBonus('essPct') / 100;

/* ---------------- Heroes ---------------- */
G.maxStars = 7;
G.maxLevel = hi => 20 + hi.stars * 10 + hi.awaken * 10;
G.levelCost = lvl => ({ essence: Math.floor(25 * Math.pow(lvl, 1.5)), gold: Math.floor(40 * Math.pow(lvl, 1.42)) });
G.evolveCost = hi => ({ shards: 10 * hi.stars, gold: 2000 * hi.stars * hi.stars });
G.awakenCost = hi => ({ stones: 2 + hi.awaken * 2, gold: 50000 * (hi.awaken + 1) });
G.skillCost = hi => ({ tomes: hi.skill * 2, gold: 1500 * hi.skill });
G.maxSkill = 10;

G.heroBase = (hi) => {
  const d = HERO_BY_ID[hi.id], c = CLASSES[d.cls].base;
  const m = RARITIES[d.rarity].mult * (1 + (hi.lvl - 1) * 0.09) * (1 + (hi.stars - 1) * 0.1);
  return { hp: c.hp * m, atk: c.atk * m, def: c.def * m, spd: c.spd, crit: c.crit, critDmg: c.critDmg };
};

G.gearOf = hi => SLOTS.map(sl => hi.equip[sl] != null ? G.gearById(hi.equip[sl]) : null).filter(Boolean);

/* Full stats including gear, awakening, artifacts and guild perks.
   `overrideGear` lets auto-equip test hypothetical loadouts. */
G.heroStats = (hi, overrideGear) => {
  const d = HERO_BY_ID[hi.id];
  const b = G.heroBase(hi);
  const add = { hp: 0, atk: 0, def: 0, spd: 0, crit: 0, critDmg: 0, hpPct: 0, atkPct: 0, defPct: 0 };
  const specials = [];
  const gear = overrideGear || G.gearOf(hi);
  for (const it of gear) {
    const gs = G.gearStats(it);
    for (const k in gs) add[k] += gs[k];
    it.specials.forEach(sp => specials.push(sp));
  }
  if (hi.awaken > 0) add[d.awaken.stat] += d.awaken.val * hi.awaken;
  for (const k of ['hpPct', 'atkPct', 'defPct', 'crit', 'critDmg', 'spd']) add[k] += G.artBonus(k);
  const gb = G.guildBonus();
  const hp = (b.hp + add.hp) * (1 + (add.hpPct + gb) / 100);
  const atk = (b.atk + add.atk) * (1 + (add.atkPct + gb) / 100);
  const def = (b.def + add.def) * (1 + add.defPct / 100);
  return {
    hp: Math.round(hp), atk: Math.round(atk), def: Math.round(def), spd: Math.round(b.spd + add.spd),
    crit: Math.min(75, b.crit + add.crit), critDmg: b.critDmg + add.critDmg, specials,
  };
};
G.statPower = st => Math.round((st.atk * 3 + st.hp * 0.25 + st.def * 2) * (1 + st.crit * st.critDmg / 10000) * (0.6 + st.spd / 250) * (1 + (st.specials ? st.specials.length : 0) * 0.04));
G.heroPower = hi => G.statPower(G.heroStats(hi));
G.teamPower = () => G.state.team.filter(Boolean).reduce((s, id) => s + (G.state.heroes[id] ? G.heroPower(G.state.heroes[id]) : 0), 0);
G.ownedCount = () => Object.keys(G.state.heroes).length;

G.levelUp = (id, times = 1) => {
  const hi = G.state.heroes[id]; let n = 0;
  while (n < times && hi.lvl < G.maxLevel(hi)) {
    const c = G.levelCost(hi.lvl);
    if (!G.spend(c)) break;
    hi.lvl++; n++;
  }
  if (n) G.track('levelup', n);
  return n;
};
G.evolve = id => {
  const hi = G.state.heroes[id];
  if (hi.stars >= G.maxStars) return 'Already at max stars.';
  const c = G.evolveCost(hi);
  if ((G.state.shards[id] || 0) < c.shards) return `Need ${c.shards} shards.`;
  if (G.state.res.gold < c.gold) return 'Not enough gold.';
  G.state.shards[id] -= c.shards; G.state.res.gold -= c.gold; hi.stars++;
  return null;
};
G.awaken = id => {
  const hi = G.state.heroes[id];
  if (hi.stars < G.maxStars) return 'Reach 7 stars to awaken.';
  if (hi.awaken >= 3) return 'Fully awakened.';
  if (!G.spend(G.awakenCost(hi))) return 'Missing: ' + G.missing(G.awakenCost(hi));
  hi.awaken++;
  return null;
};
G.skillUp = id => {
  const hi = G.state.heroes[id];
  if (hi.skill >= G.maxSkill) return 'Skill is maxed.';
  if (!G.spend(G.skillCost(hi))) return 'Missing: ' + G.missing(G.skillCost(hi));
  hi.skill++;
  return null;
};
G.unlockHero = id => {
  const need = RARITIES[HERO_BY_ID[id].rarity].unlock;
  if ((G.state.shards[id] || 0) < need) return `Need ${need} shards.`;
  G.state.shards[id] -= need;
  G.state.heroes[id] = G.makeHeroInst(id);
  return null;
};
/* Called by summons: new hero or shards if duplicate */
G.obtainHero = id => {
  if (G.state.heroes[id]) {
    const n = RARITIES[HERO_BY_ID[id].rarity].dupe;
    G.state.shards[id] = (G.state.shards[id] || 0) + n;
    return { dupe: true, shards: n };
  }
  G.state.heroes[id] = G.makeHeroInst(id);
  return { dupe: false };
};
G.setTeamSlot = (slot, id) => {
  const t = G.state.team;
  const cur = t.indexOf(id);
  if (id && cur >= 0) t[cur] = t[slot] || null; // swap
  t[slot] = id;
};

/* ---------------- Equipment ---------------- */
const RAR_GEAR_MULT = [1, 1.3, 1.7, 2.3, 3.1];
G.gearById = uid => G.state.gear.find(g => g.uid === uid);
G.ilvlScale = (ilvl, rarity) => (1 + (ilvl - 1) * 0.18) * RAR_GEAR_MULT[rarity];
G.maxEnh = it => 5 + it.rarity * 5;

G.makeGear = (slot, rarity, ilvl) => {
  const s = G.state;
  const sc = G.ilvlScale(ilvl, rarity);
  const nAff = [1, 2, 3, 3, 4][rarity];
  const pool = AFFIX_POOL.slice();
  const affixes = [];
  for (let i = 0; i < nAff && pool.length; i++) {
    const a = pool.splice(U.randi(0, pool.length - 1), 1)[0];
    let v = U.rand(a.min, a.max) * (1 + rarity * 0.3);
    if (a.scaled) v *= sc / RAR_GEAR_MULT[rarity];
    affixes.push({ stat: a.stat, val: a.stat === 'spd' || a.scaled ? Math.round(v) : Math.round(v * 10) / 10 });
  }
  const specials = [];
  const nSp = rarity === 3 ? 1 : rarity === 4 ? 2 : 0;
  const sp = SPECIAL_KEYS.slice();
  for (let i = 0; i < nSp; i++) specials.push(sp.splice(U.randi(0, sp.length - 1), 1)[0]);
  const base = U.pick(SLOT_INFO[slot].bases);
  const pre = U.pick(RARITY_PREFIX[rarity]);
  const name = specials.length ? `${SPECIALS[specials[0]].name} ${pre} ${base}` : `${pre} ${base}`;
  const it = { uid: s.nextUid++, slot, rarity, ilvl, enh: 0, affixes, specials, name, variant: U.randi(0, 5), locked: false, owner: null };
  s.gear.push(it);
  return it;
};

G.gearMain = it => {
  const sc = G.ilvlScale(it.ilvl, it.rarity) * (1 + it.enh * 0.1);
  switch (it.slot) {
    case 'weapon': return { atk: Math.round(24 * sc) };
    case 'helmet': return { hp: Math.round(170 * sc), def: Math.round(7 * sc) };
    case 'armor': return { hp: Math.round(260 * sc), def: Math.round(13 * sc) };
    case 'boots': return { hp: Math.round(120 * sc), spd: 3 + it.rarity * 2 + Math.floor(it.enh / 3) };
    case 'accessory': return { atk: Math.round(11 * sc), crit: 2 + it.rarity * 1.5 };
  }
  return {};
};
G.gearStats = it => {
  const out = Object.assign({}, G.gearMain(it));
  for (const a of it.affixes) out[a.stat] = (out[a.stat] || 0) + a.val;
  return out;
};
G.gearScore = it => {
  const st = G.gearStats(it);
  const sc = G.ilvlScale(it.ilvl, 0) * 100; // reference magnitude for % stats
  return Math.round((st.atk || 0) * 3 + (st.hp || 0) * 0.25 + (st.def || 0) * 2 + (st.spd || 0) * 12 +
    ((st.atkPct || 0) * sc * 0.03) + ((st.hpPct || 0) * sc * 0.03) + ((st.defPct || 0) * sc * 0.015) +
    (st.crit || 0) * sc * 0.03 + (st.critDmg || 0) * sc * 0.012 + it.specials.length * sc * 0.6);
};
G.equip = (heroId, uid) => {
  const it = G.gearById(uid); const hi = G.state.heroes[heroId];
  if (!it || !hi) return;
  if (it.owner) G.unequip(uid);
  const prev = hi.equip[it.slot];
  if (prev != null) { const p = G.gearById(prev); if (p) p.owner = null; }
  hi.equip[it.slot] = uid; it.owner = heroId;
};
G.unequip = uid => {
  const it = G.gearById(uid); if (!it || !it.owner) return;
  const hi = G.state.heroes[it.owner];
  if (hi && hi.equip[it.slot] === uid) hi.equip[it.slot] = null;
  it.owner = null;
};
/* Picks the loadout maximising hero power among free items + current gear */
G.autoEquip = heroId => {
  const hi = G.state.heroes[heroId]; let changed = 0;
  for (const slot of SLOTS) {
    const cands = G.state.gear.filter(g => g.slot === slot && (!g.owner || g.owner === heroId));
    if (!cands.length) continue;
    const others = G.gearOf(hi).filter(g => g.slot !== slot);
    let best = null, bestP = G.statPower(G.heroStats(hi, others));
    for (const c of cands) {
      const p = G.statPower(G.heroStats(hi, others.concat([c])));
      if (p > bestP) { bestP = p; best = c; }
    }
    if (best && hi.equip[slot] !== best.uid) { G.equip(heroId, best.uid); changed++; }
  }
  return changed;
};
G.enhanceCost = it => ({ steel: Math.ceil(4 * (it.enh + 1) * (1 + it.rarity * 0.6)), gold: Math.ceil(120 * (it.enh + 1) * Math.sqrt(it.ilvl) * (1 + it.rarity * 0.5)) });
G.enhance = uid => {
  const it = G.gearById(uid);
  if (it.enh >= G.maxEnh(it)) return 'Max enhancement reached.';
  const c = G.enhanceCost(it);
  if (!G.spend(c)) return 'Missing: ' + G.missing(c);
  it.enh++;
  let bonus = null;
  if (it.enh % 5 === 0 && it.affixes.length) {
    const a = U.pick(it.affixes); a.val = a.stat === 'spd' || a.stat === 'atk' || a.stat === 'hp' ? Math.ceil(a.val * 1.25) : Math.round(a.val * 1.25 * 10) / 10;
    bonus = a;
  }
  G.state.stats.enhanced++;
  G.track('enhance', 1);
  return { bonus };
};
G.salvageValue = it => ({ steel: Math.round((3 + it.rarity * 6) * (1 + it.ilvl * 0.03)) + it.enh * 3, gold: Math.round(80 * (1 + it.rarity) * Math.sqrt(it.ilvl)) });
G.salvage = uids => {
  const total = { steel: 0, gold: 0 };
  const set = new Set(uids);
  G.state.gear = G.state.gear.filter(it => {
    if (!set.has(it.uid) || it.locked || it.owner) return true;
    const v = G.salvageValue(it); total.steel += v.steel; total.gold += v.gold;
    return false;
  });
  G.grant(total);
  return total;
};
/* Combine 3 free items of same slot & rarity into 1 of next rarity */
G.combinable = (slot, rarity) => G.state.gear.filter(g => g.slot === slot && g.rarity === rarity && !g.owner && !g.locked).sort((a, b) => a.ilvl - b.ilvl);
G.combine = (slot, rarity) => {
  if (rarity >= 4) return null;
  const c = G.combinable(slot, rarity);
  if (c.length < 3) return null;
  const used = c.slice(0, 3);
  const ilvl = Math.max(...used.map(u => u.ilvl)) + 1;
  const ids = new Set(used.map(u => u.uid));
  G.state.gear = G.state.gear.filter(g => !ids.has(g.uid));
  const it = G.makeGear(slot, rarity + 1, ilvl);
  if (it.rarity === 4) G.state.stats.mythic++;
  return it;
};

/* ---------------- Forge ---------------- */
G.forgeOdds = (lvl = G.state.forge.lvl) => {
  const luck = 1 + G.artBonus('forgeLuck') * 0.06;
  const w = [Math.max(4, 70 - 2.6 * lvl), 24 + 0.4 * lvl, (5 + 1.1 * lvl) * luck, (0.8 + 0.42 * lvl) * luck, (0.06 + 0.075 * lvl) * luck];
  const t = w.reduce((a, b) => a + b, 0);
  return w.map(x => x / t * 100);
};
G.forgeMax = 30;
G.forgeUpgradeCost = () => ({ gold: Math.floor(1500 * Math.pow(1.55, G.state.forge.lvl - 1)) });
G.forgeIlvl = () => 1 + Math.floor((G.state.stage - 1) / 3) + (G.state.forge.lvl - 1) * 2;
G.forge = n => {
  const out = [];
  for (let i = 0; i < n; i++) {
    if (G.state.res.ore < 1) break;
    G.state.res.ore--;
    const r = U.weighted(G.forgeOdds());
    const it = G.makeGear(U.pick(SLOTS), r, Math.max(1, G.forgeIlvl() + U.randi(-1, 1)));
    if (r === 4) G.state.stats.mythic++;
    out.push(it);
  }
  G.state.stats.forged += out.length;
  G.state.events.festForged += out.length;
  G.track('forge', out.length);
  return out;
};

/* ---------------- Campaign ---------------- */
G.enemyMult = n => 0.9 * (1 + 0.08 * (n - 1)) * Math.pow(1.0045, n - 1);
G.stageInfo = n => {
  const ch = Math.floor((n - 1) / STAGES_PER_CHAPTER);
  const idx = (n - 1) % STAGES_PER_CHAPTER + 1;
  const isBoss = idx % 10 === 0;
  return { n, ch, idx, isBoss, chapterName: CHAPTERS[Math.min(ch, CHAPTERS.length - 1)], bossName: BOSS_NAMES[Math.min(ch, BOSS_NAMES.length - 1)] + (idx === 10 ? ' (Vanguard)' : '') };
};
G.chapterTint = (kind, ch) => {
  const base = MONSTER_KINDS[kind].col; const cyc = Math.floor(ch / 10);
  if (cyc === 0) return base;
  const mixWith = cyc === 1 ? '#7a2aff' : '#ff2a1a';
  const a = parseInt(base.slice(1), 16), b = parseInt(mixWith.slice(1), 16), f = 0.4;
  const m = (sh) => Math.round(((a >> sh) & 255) * (1 - f) + ((b >> sh) & 255) * f);
  return '#' + ((1 << 24) + (m(16) << 16) + (m(8) << 8) + m(0)).toString(16).slice(1);
};
G.monsterUnit = (kind, mult, opts = {}) => {
  const k = MONSTER_KINDS[kind];
  const boss = !!opts.boss;
  const hpM = boss ? 5.5 : 1, atkM = boss ? 1.5 : 1;
  return {
    name: opts.name || k.name, kind, isBoss: boss, isMonster: true,
    sprite: ART.monster(kind, opts.col || k.col, boss),
    cls: k.ranged ? 'mage' : 'warrior', ranged: k.ranged,
    hp: Math.round(MONSTER_BASE.hp * k.hp * mult * hpM), atk: Math.round(MONSTER_BASE.atk * k.atk * mult * atkM),
    def: Math.round(MONSTER_BASE.def * k.def * mult), spd: k.spd + (boss ? 10 : 0),
    crit: MONSTER_BASE.crit, critDmg: MONSTER_BASE.critDmg, specials: boss ? ['bulwark'] : [],
    skill: boss ? { name: 'Tyrant\'s Wrath', type: 'aoe', mult: 1.1, stun: { chance: 0.3, dur: 1 } }
      : k.ranged ? { name: 'Hex Bolt', type: 'multi', hits: 2, mult: 0.9 } : { name: 'Savage Maul', type: 'strike', mult: 1.7 },
  };
};
G.stageEnemies = n => {
  const info = G.stageInfo(n), ch = info.ch;
  const pool = [MONSTER_KEYS[ch % 10], MONSTER_KEYS[(ch + 3) % 10], MONSTER_KEYS[(ch + 7) % 10]];
  const rng = U.seeded(n * 7919);
  const pickK = () => pool[Math.floor(rng() * pool.length)];
  const mult = G.enemyMult(n);
  const count = n <= 2 ? 3 : n <= 6 ? 4 : 5;
  const units = [];
  if (info.isBoss) {
    const bk = info.idx === 20 ? (ch % 2 ? 'dragon' : 'demon') : pool[0];
    for (let i = 0; i < 5; i++) {
      if (i === 0) units.push(G.monsterUnit(bk, mult, { boss: true, name: info.bossName, col: G.chapterTint(bk, ch) }));
      else if (i <= (n < 30 ? 2 : 4)) { const kk = pickK(); units.push(G.monsterUnit(kk, mult * 0.9, { col: G.chapterTint(kk, ch) })); }
      else units.push(null);
    }
  } else {
    for (let i = 0; i < 5; i++) {
      if (i < count) { const kk = pickK(); units.push(G.monsterUnit(kk, mult, { col: G.chapterTint(kk, ch) })); } else units.push(null);
    }
  }
  return units;
};
G.stagePower = n => G.stageEnemies(n).filter(Boolean).reduce((s, u) => s + G.statPower(u), 0);
G.stageRewards = n => {
  const info = G.stageInfo(n);
  const r = {
    gold: Math.round(150 * (1 + n * 0.25) * G.goldMult()),
    essence: Math.round((250 + n * 60) * G.essMult()),
    ore: 2 + Math.floor(n / 20),
    steel: 4 + Math.floor(n / 8),
  };
  if (info.isBoss) {
    r.gems = 40 + Math.floor(n / 10) * 4; r.artFrags = 3 + Math.floor(n / 50); r.tomes = 1 + Math.floor(n / 100);
    r.gold *= 3; r.ore += 5;
    if (info.idx === 20) { r.scrolls = 1; r.mysteryChest = 1; }
    if (n % 50 === 0) r.stones = 1;
  }
  return r;
};
G.clearStage = n => {
  const rew = G.stageRewards(n);
  G.grant(rew);
  const drops = [];
  const info = G.stageInfo(n);
  if (info.isBoss || U.chance(0.35)) {
    const r = info.isBoss ? Math.max(1, U.weighted(G.forgeOdds())) : U.weighted(G.forgeOdds());
    drops.push(G.makeGear(U.pick(SLOTS), r, Math.max(1, G.forgeIlvl())));
  }
  if (info.isBoss) G.state.stats.bosses++;
  G.state.stats.battles++;
  if (n === G.state.stage && G.state.stage < MAX_STAGE + 1) G.state.stage++;
  G.track('win', 1);
  return { rew, drops };
};
G.playerLevel = () => 1 + Math.floor((G.state.stage - 1) / 4);

/* ---------------- AFK ---------------- */
G.afkRates = () => { // per hour
  const s = Math.max(1, G.state.stage - 1);
  const m = 1 + G.artBonus('afkPct') / 100;
  return {
    gold: Math.round(900 * (1 + s * 0.15) * G.goldMult() * m),
    essence: Math.round(1500 * (1 + s * 0.08) * G.essMult() * m),
    ore: Math.round((5 + s / 12) * m),
    steel: Math.round((10 + s / 4) * m),
    gems: Math.round(4 * m),
  };
};
G.afkSeconds = () => Math.min(AFK_CAP_H * 3600, (Date.now() - G.state.afkSince) / 1000);
G.afkPending = (secs = G.afkSeconds()) => {
  const r = G.afkRates(), h = secs / 3600;
  return { gold: Math.floor(r.gold * h), essence: Math.floor(r.essence * h), ore: Math.floor(r.ore * h), steel: Math.floor(r.steel * h), gems: Math.floor(r.gems * h) };
};
G.collectAfk = () => {
  const secs = G.afkSeconds();
  const rew = G.afkPending(secs);
  const drops = [];
  const nItems = Math.floor(secs / 3600 / 1.5);
  for (let i = 0; i < Math.min(nItems, 8); i++) drops.push(G.makeGear(U.pick(SLOTS), U.weighted(G.forgeOdds()), G.forgeIlvl()));
  G.grant(rew);
  G.state.afkSince = Date.now();
  G.track('afk', 1);
  return { rew, drops, secs };
};
G.quickLootCost = () => G.state.quickLootFree > 0 ? null : { gems: 50 };
G.quickLoot = () => {
  const c = G.quickLootCost();
  if (c && !G.spend(c)) return null;
  if (!c) G.state.quickLootFree--;
  const rew = G.afkPending(2 * 3600);
  G.grant(rew);
  return rew;
};

/* ---------------- Battle units ---------------- */
G.heroUnit = (hi, scale = 1) => {
  const d = HERO_BY_ID[hi.id];
  const st = G.heroStats(hi);
  return {
    name: d.name, heroId: d.id, cls: d.cls, rarity: d.rarity, sprite: ART.hero(d.id),
    ranged: d.cls === 'mage' || d.cls === 'archer' || d.cls === 'support',
    hp: Math.round(st.hp * scale), atk: Math.round(st.atk * scale), def: Math.round(st.def * scale), spd: st.spd,
    crit: st.crit, critDmg: st.critDmg, specials: st.specials,
    skill: Object.assign({}, d.skill, { mult: d.skill.mult * (1 + (hi.skill - 1) * 0.1), shieldAll: d.skill.shieldAll ? d.skill.shieldAll * (1 + (hi.skill - 1) * 0.1) : 0 }),
  };
};
G.teamUnits = () => G.state.team.map(id => id && G.state.heroes[id] ? G.heroUnit(G.state.heroes[id]) : null);

/* Virtual team for simulated opponents, scaled to a target power */
G.virtualTeam = (seed, targetPower, size = 5) => {
  const rng = U.seeded(seed);
  const ids = HEROES.map(h => h.id).sort(() => rng() - 0.5);
  const chosen = [];
  // ensure sensible composition: at least one warrior up front
  const warriors = ids.filter(i => HERO_BY_ID[i].cls === 'warrior');
  chosen.push(warriors[0]);
  for (const id of ids) { if (chosen.length >= size) break; if (!chosen.includes(id)) chosen.push(id); }
  const tierBias = U.clamp(targetPower / 60000, 0, 4);
  const units = chosen.map((id, i) => {
    const hi = G.makeHeroInst(id);
    hi.lvl = Math.max(1, Math.round(10 + tierBias * 15 + rng() * 10));
    hi.stars = Math.min(7, HERO_BY_ID[id].rarity + 1 + Math.floor(rng() * 2));
    hi.skill = 1 + Math.floor(rng() * 4 + tierBias);
    const d = HERO_BY_ID[id], st = G.heroBase(hi);
    return {
      name: d.name, heroId: id, cls: d.cls, rarity: d.rarity, sprite: ART.hero(id),
      ranged: d.cls === 'mage' || d.cls === 'archer' || d.cls === 'support',
      hp: st.hp, atk: st.atk, def: st.def, spd: st.spd, crit: st.crit, critDmg: st.critDmg,
      specials: rng() < tierBias / 4 ? [SPECIAL_KEYS[Math.floor(rng() * SPECIAL_KEYS.length)]] : [],
      skill: Object.assign({}, d.skill, { mult: d.skill.mult * (1 + (hi.skill - 1) * 0.1) }),
    };
  });
  // put warriors front
  units.sort((a, b) => (a.cls === 'warrior' ? 0 : 1) - (b.cls === 'warrior' ? 0 : 1));
  const raw = units.reduce((s, u) => s + G.statPower(u), 0);
  const k = targetPower / raw;
  units.forEach(u => { u.hp = Math.round(u.hp * k); u.atk = Math.round(u.atk * k); u.def = Math.round(u.def * k); });
  return units;
};
G.unitsPower = units => units.filter(Boolean).reduce((s, u) => s + G.statPower(u), 0);

/* ---------------- Arena ---------------- */
G.ARENA_TIERS = [
  { name: 'Bronze', min: 0, col: '#c08050', gems: 100, coins: 150, scrolls: 0 },
  { name: 'Silver', min: 1100, col: '#c8d0dc', gems: 200, coins: 250, scrolls: 1 },
  { name: 'Gold', min: 1300, col: '#ffcf4a', gems: 350, coins: 400, scrolls: 1 },
  { name: 'Platinum', min: 1500, col: '#6ae8e0', gems: 500, coins: 600, scrolls: 2 },
  { name: 'Diamond', min: 1750, col: '#7ab8ff', gems: 800, coins: 900, scrolls: 3 },
  { name: 'Champion', min: 2000, col: '#ff5a7a', gems: 1200, coins: 1400, scrolls: 5 },
];
G.arenaTier = rating => { let t = G.ARENA_TIERS[0]; for (const x of G.ARENA_TIERS) if (rating >= x.min) t = x; return t; };
G.fakeName = rng => NAME_A[Math.floor(rng() * NAME_A.length)] + NAME_B[Math.floor(rng() * NAME_B.length)] + (rng() < 0.4 ? Math.floor(rng() * 99) : '');
G.genOpponents = () => {
  const a = G.state.arena; a.refresh++;
  const myP = Math.max(1000, G.teamPower());
  const out = [];
  const factors = [0.75, 0.92, 1.05, 1.25];
  factors.forEach((f, i) => {
    const seed = U.hash(a.week + ':' + a.refresh + ':' + i);
    const rng = U.seeded(seed);
    const power = Math.round(myP * f * (0.95 + rng() * 0.1));
    out.push({ seed, name: G.fakeName(rng), power, rating: Math.max(800, Math.round(a.rating + (f - 1) * 400 + (rng() - 0.5) * 60)), avatar: HEROES[Math.floor(rng() * HEROES.length)].id, done: false });
  });
  a.opps = out;
};
G.leaderboard = () => {
  const a = G.state.arena;
  const rng = U.seeded(U.hash('lb' + a.week));
  const list = [];
  for (let i = 0; i < 49; i++) {
    const rating = Math.round(2600 - i * 32 - rng() * 25);
    list.push({ name: G.fakeName(rng), rating, power: Math.round(rating * rating * (0.05 + rng() * 0.03)), avatar: HEROES[Math.floor(rng() * HEROES.length)].id });
  }
  list.push({ name: G.state.name, rating: a.rating, power: G.teamPower(), avatar: G.state.team.find(Boolean) || 'torvald', me: true });
  list.sort((x, y) => y.rating - x.rating);
  return list;
};
G.arenaResult = (opp, win) => {
  const a = G.state.arena;
  const diff = (opp.rating - a.rating) / 400;
  const delta = win ? Math.round(U.clamp(22 + diff * 12, 8, 40)) : -Math.round(U.clamp(14 - diff * 6, 5, 22));
  a.rating = Math.max(0, a.rating + delta);
  a.best = Math.max(a.best, a.rating);
  if (win) { a.wins++; G.state.stats.arenaWins++; } else a.losses++;
  const coins = win ? 30 : 10;
  G.grant({ arenaCoins: coins, gold: win ? Math.round(1000 * G.goldMult() * (1 + G.state.stage * 0.05)) : 0 });
  a.log.unshift({ name: opp.name, win, delta, t: Date.now() });
  a.log = a.log.slice(0, 20);
  opp.done = true;
  G.track('arena', 1);
  return { delta, coins };
};

/* ---------------- Guild ---------------- */
G.guildCatalog = () => GUILD_NAMES.map(([name, icon], i) => {
  const rng = U.seeded(U.hash(name));
  const lvl = 2 + Math.floor(rng() * 12);
  return { id: i, name, icon, lvl, members: 12 + Math.floor(rng() * 17), power: Math.round(Math.pow(lvl, 2.2) * 9000 * (0.8 + rng() * 0.4)), req: Math.round(lvl * 1200 * (0.5 + rng())) };
});
G.makeGuild = (name, icon, lvl, memberCount) => {
  const rng = U.seeded(U.hash(name + Date.now()));
  const members = [];
  for (let i = 0; i < memberCount; i++) {
    members.push({ name: G.fakeName(rng), role: i === 0 ? 'Leader' : i < 3 ? 'Officer' : 'Member', power: Math.round(G.teamPower() * (0.4 + rng() * 1.4)), contrib: Math.floor(rng() * 800), avatar: HEROES[Math.floor(rng() * HEROES.length)].id });
  }
  const gd = { name, icon, lvl, xp: 0, members, bossLvl: 1, bossMax: 0, bossHp: 0, bossKills: 0, attempts: 3, donations: 0, myContrib: 0, myBestDmg: 0, warAttacks: 2, war: null, pendingWar: null, joined: Date.now() };
  G.state.guild = gd;
  G.resetGuildBoss();
  return gd;
};
G.joinGuild = cat => {
  if (G.state.guild) return 'Leave your current guild first.';
  if (G.teamPower() < cat.req) return `Requires ${U.fmt(cat.req)} team power.`;
  G.makeGuild(cat.name, cat.icon, cat.lvl, cat.members);
  return null;
};
G.createGuild = (name, icon) => {
  if (!name || name.trim().length < 3) return 'Name must be at least 3 characters.';
  if (!G.spend({ gems: 300 })) return 'Creating a guild costs 300 gems.';
  G.makeGuild(name.trim().slice(0, 24), icon, 1, 0);
  return null;
};
G.leaveGuild = () => { G.state.guild = null; };
G.guildXpNeed = lvl => 600 * lvl;
G.guildLevelCheck = () => {
  const gd = G.state.guild; if (!gd) return;
  while (gd.lvl < 30 && gd.xp >= G.guildXpNeed(gd.lvl)) { gd.xp -= G.guildXpNeed(gd.lvl); gd.lvl++; }
};
G.guildBossName = () => GUILD_BOSSES[(G.state.guild.bossLvl - 1) % GUILD_BOSSES.length];
G.resetGuildBoss = () => {
  const gd = G.state.guild;
  const base = Math.max(G.stagePower(Math.max(1, G.state.stage)), G.teamPower()) * 90;
  gd.bossMax = Math.round(base * Math.pow(1.35, gd.bossLvl - 1) * (1 + gd.members.length * 0.05));
  gd.bossHp = gd.bossMax;
};
G.guildBossDamage = (dmg, mine = true) => {
  const gd = G.state.guild; if (!gd) return null;
  gd.bossHp -= dmg;
  if (mine) { gd.myContrib += Math.round(dmg / gd.bossMax * 1000); gd.myBestDmg = Math.max(gd.myBestDmg, dmg); }
  if (gd.bossHp <= 0) {
    gd.bossKills++; gd.bossLvl++;
    const rew = { gems: 150 + gd.bossLvl * 20, guildCoins: 300, artFrags: 8, stones: gd.bossLvl % 3 === 0 ? 1 : 0 };
    G.grant(rew); gd.xp += 200; G.guildLevelCheck();
    G.resetGuildBoss();
    return rew;
  }
  return null;
};
G.guildBossUnit = () => {
  const gd = G.state.guild;
  const mult = G.enemyMult(Math.max(1, G.state.stage - 1)) * 1.0;
  const u = G.monsterUnit(['dragon', 'golem', 'spider', 'wraith', 'demon'][(gd.bossLvl - 1) % 5], mult, { boss: true, name: G.guildBossName() });
  u.hp = gd.bossHp; u.maxHpOverride = gd.bossMax; u.def = Math.round(u.def * 0.6);
  u.specials = [];
  return u;
};
G.DONATIONS = [
  { key: 'gold', label: 'Gold Donation', cost: () => ({ gold: 5000 + G.state.stage * 200 }), xp: 20, coins: 40 },
  { key: 'gems', label: 'Gem Donation', cost: () => ({ gems: 50 }), xp: 60, coins: 120 },
  { key: 'ore', label: 'Ore Donation', cost: () => ({ ore: 20 }), xp: 35, coins: 70 },
];
G.donate = idx => {
  const gd = G.state.guild; const d = G.DONATIONS[idx];
  if (gd.donations >= 3) return 'Daily donation limit reached (3).';
  if (!G.spend(d.cost())) return 'Missing: ' + G.missing(d.cost());
  gd.donations++; gd.xp += d.xp; gd.myContrib += d.xp; G.grant({ guildCoins: d.coins }); G.guildLevelCheck();
  G.track('guild', 1);
  return null;
};
G.ensureWar = () => {
  const gd = G.state.guild; if (!gd) return null;
  if (!gd.war) {
    const cat = G.guildCatalog().filter(c => c.name !== gd.name);
    const rng = U.seeded(U.hash(G.state.arena.week + gd.name));
    const enemy = cat[Math.floor(rng() * cat.length)];
    const p = Math.max(1000, G.teamPower());
    gd.war = {
      enemy: enemy.name, enemyIcon: enemy.icon, myStars: 0, allyStars: 0, enemyStars: Math.round(6 + rng() * 10),
      defenders: [0.85, 1.0, 1.2].map((f, i) => ({ name: G.fakeName(rng), power: Math.round(p * f), seed: U.hash(enemy.name + i + G.state.arena.week), stars: 0 })),
    };
  }
  return gd.war;
};
G.warResult = (idx, alive) => {
  const w = G.state.guild.war; const d = w.defenders[idx];
  const stars = alive >= 4 ? 3 : alive >= 2 ? 2 : alive >= 1 ? 1 : 0;
  const gained = Math.max(0, stars - d.stars);
  d.stars = Math.max(d.stars, stars);
  w.myStars += gained;
  // allies and enemies progress a bit with each attack
  w.allyStars += U.randi(1, 3); w.enemyStars += U.randi(0, 2);
  G.grant({ guildCoins: 40 + gained * 30 });
  G.track('guild', 1);
  return { stars, gained };
};
G.guildRanking = () => {
  const list = G.guildCatalog().map(c => ({ name: c.name, icon: c.icon, lvl: c.lvl, power: c.power }));
  if (G.state.guild) {
    const gd = G.state.guild;
    const idx = list.findIndex(x => x.name === gd.name);
    const mine = { name: gd.name, icon: gd.icon, lvl: gd.lvl, power: G.teamPower() + gd.members.reduce((s, m) => s + m.power, 0), me: true };
    if (idx >= 0) list[idx] = mine; else list.push(mine);
  }
  return list.sort((a, b) => b.power - a.power);
};

/* ---------------- Artifacts ---------------- */
G.addArtFrags = (id, n) => {
  const a = G.state.artifacts[id] || (G.state.artifacts[id] = { lvl: 0, frags: 0 });
  a.frags += n;
};
G.artCost = id => { const l = G.artLevel(id); return { frags: l === 0 ? 10 : 10 + l * 8, gold: 5000 * Math.pow(2, l) }; };
G.upgradeArtifact = id => {
  const a = G.state.artifacts[id] || (G.state.artifacts[id] = { lvl: 0, frags: 0 });
  if (a.lvl >= ARTIFACT_MAX) return 'Artifact is maxed.';
  const c = G.artCost(id);
  if (a.frags < c.frags) return `Need ${c.frags} fragments.`;
  if (G.state.res.gold < c.gold) return 'Not enough gold.';
  a.frags -= c.frags; G.state.res.gold -= c.gold; a.lvl++;
  return null;
};
G.dustToFrag = (id, n = 1) => {
  if (G.state.res.artFrags < n * 2) return 'Need 2 Relic Dust per fragment.';
  G.state.res.artFrags -= n * 2; G.addArtFrags(id, n);
  return null;
};

/* ---------------- Summoning ---------------- */
G.SUMMON_RATES = [50, 32, 13.5, 4, 0.5];
G.summon = (n, payWith) => {
  const cost = payWith === 'scrolls' ? { scrolls: n } : { gems: n === 10 ? 1350 : 150 };
  if (!G.spend(cost)) return null;
  const out = [];
  for (let i = 0; i < n; i++) {
    G.state.pity++;
    let r = U.weighted(G.SUMMON_RATES);
    if (G.state.pity >= 60 && r < 3) r = 3;
    if (n === 10 && i === 9 && !out.some(o => o.rarity >= 2) && r < 2) r = 2;
    if (r >= 3) G.state.pity = 0;
    const pool = HEROES.filter(h => h.rarity === r);
    const h = U.pick(pool);
    const res = G.obtainHero(h.id);
    out.push({ id: h.id, rarity: r, ...res });
  }
  G.state.stats.summoned += n;
  G.track('summon', n);
  return out;
};

/* ---------------- Missions & Achievements ---------------- */
G.track = (key, n = 1) => { const p = G.state.daily.prog; p[key] = (p[key] || 0) + n; };
G.activityPoints = () => DAILY_MISSIONS.reduce((s, m) => s + (G.state.daily.claimed[m.key] ? m.pts : 0), 0);
G.claimMission = key => {
  const m = DAILY_MISSIONS.find(x => x.key === key);
  if (G.state.daily.claimed[key] || (G.state.daily.prog[key] || 0) < m.goal) return false;
  G.state.daily.claimed[key] = true;
  G.grant({ gold: 2000 + G.state.stage * 100, gems: 10 });
  return true;
};
G.claimChest = i => {
  const c = ACTIVITY_CHESTS[i];
  if (G.state.daily.chests.includes(i) || G.activityPoints() < c.at) return null;
  G.state.daily.chests.push(i); G.grant(c.reward); return c.reward;
};
G.achValue = key => {
  const s = G.state;
  switch (key) {
    case 'stage': return s.stage - 1;
    case 'forged': return s.stats.forged;
    case 'enhanced': return s.stats.enhanced;
    case 'heroes': return G.ownedCount();
    case 'summoned': return s.stats.summoned;
    case 'arenaWins': return s.stats.arenaWins;
    case 'bosses': return s.stats.bosses;
    case 'power': return G.teamPower();
    case 'mythic': return s.stats.mythic;
  }
  return 0;
};
G.achReward = tierIdx => ({ gems: 30 + tierIdx * 30, gold: 3000 * (tierIdx + 1) });
G.claimAch = key => {
  const a = ACHIEVEMENTS.find(x => x.key === key);
  const t = G.state.ach[key] || 0;
  if (t >= a.tiers.length || G.achValue(key) < a.tiers[t]) return null;
  G.state.ach[key] = t + 1;
  const r = G.achReward(t); G.grant(r); return r;
};
G.achClaimable = () => ACHIEVEMENTS.some(a => { const t = G.state.ach[a.key] || 0; return t < a.tiers.length && G.achValue(a.key) >= a.tiers[t]; });
G.missionsClaimable = () => DAILY_MISSIONS.some(m => !G.state.daily.claimed[m.key] && (G.state.daily.prog[m.key] || 0) >= m.goal) || ACTIVITY_CHESTS.some((c, i) => !G.state.daily.chests.includes(i) && G.activityPoints() >= c.at);

/* ---------------- Login rewards ---------------- */
G.canClaimLogin = () => G.state.login.last !== U.dayKey();
G.claimLogin = () => {
  if (!G.canClaimLogin()) return null;
  const idx = G.state.login.count % 7;
  const r = LOGIN_REWARDS[idx];
  G.grant(r); G.state.login.count++; G.state.login.last = U.dayKey();
  return r;
};

/* ---------------- Shop ---------------- */
G.DEAL_POOL = [
  { give: { ore: 20 }, cost: { gold: 6000 } }, { give: { ore: 50 }, cost: { gems: 120 } },
  { give: { steel: 80 }, cost: { gold: 8000 } }, { give: { steel: 200 }, cost: { gems: 150 } },
  { give: { essence: 20000 }, cost: { gold: 10000 } }, { give: { tomes: 3 }, cost: { gems: 90 } },
  { give: { scrolls: 1 }, cost: { gems: 120 } }, { give: { artFrags: 6 }, cost: { gems: 100 } },
  { give: { stones: 1 }, cost: { gems: 300 } }, { give: { heroChest: 1 }, cost: { gems: 250 } },
  { give: { mysteryChest: 1 }, cost: { gold: 15000 } }, { give: { gold: 30000 }, cost: { gems: 100 } },
];
G.ensureDeals = () => {
  const sh = G.state.shop;
  if (sh.day !== G.state.day || !sh.deals.length) {
    sh.day = G.state.day; sh.bought = {};
    const rng = U.seeded(U.hash(G.state.day));
    const pool = G.DEAL_POOL.map((d, i) => i).sort(() => rng() - 0.5).slice(0, 6);
    sh.deals = pool.map(i => {
      const d = G.DEAL_POOL[i]; const scale = 1 + Math.floor(G.state.stage / 40) * 0.5;
      const give = {}; for (const k in d.give) give[k] = k === 'gold' || k === 'essence' ? Math.round(d.give[k] * scale) : d.give[k];
      return { give, cost: d.cost, off: [0, 10, 20, 30][Math.floor(rng() * 4)] };
    });
  }
  return sh.deals;
};
G.dealCost = d => { const c = {}; for (const k in d.cost) c[k] = Math.round(d.cost[k] * (1 - d.off / 100)); return c; };
G.buy = (cost, give) => { if (!G.spend(cost)) return false; G.grant(give); return true; };
G.ARENA_SHOP = [
  { id: 'a1', give: { scrolls: 1 }, cost: { arenaCoins: 300 }, limit: 5 },
  { id: 'a2', give: { stones: 1 }, cost: { arenaCoins: 600 }, limit: 2 },
  { id: 'a3', give: { 'shard:kestrel': 10 }, cost: { arenaCoins: 500 }, limit: 5 },
  { id: 'a4', give: { 'shard:rhea': 10 }, cost: { arenaCoins: 500 }, limit: 5 },
  { id: 'a5', give: { steel: 150 }, cost: { arenaCoins: 150 }, limit: 10 },
  { id: 'a6', give: { 'art:raven_eye': 5 }, cost: { arenaCoins: 400 }, limit: 3 },
];
G.GUILD_SHOP = [
  { id: 'g1', give: { 'shard:lumen': 10 }, cost: { guildCoins: 800 }, limit: 5 },
  { id: 'g2', give: { 'shard:seraphine': 10 }, cost: { guildCoins: 500 }, limit: 5 },
  { id: 'g3', give: { tomes: 3 }, cost: { guildCoins: 200 }, limit: 10 },
  { id: 'g4', give: { ore: 40 }, cost: { guildCoins: 250 }, limit: 10 },
  { id: 'g5', give: { 'art:aegis_shard': 5 }, cost: { guildCoins: 400 }, limit: 3 },
  { id: 'g6', give: { 'art:forge_soul': 5 }, cost: { guildCoins: 500 }, limit: 3 },
];
G.EVENT_SHOP = [
  { id: 'e1', give: { 'shard:valka': 15 }, cost: { moonTokens: 600 }, limit: 10 },
  { id: 'e2', give: { 'art:moon_sigil': 5 }, cost: { moonTokens: 400 }, limit: 4 },
  { id: 'e3', give: { gems: 200 }, cost: { moonTokens: 300 }, limit: 5 },
  { id: 'e4', give: { scrolls: 2 }, cost: { moonTokens: 350 }, limit: 5 },
  { id: 'e5', give: { stones: 1 }, cost: { moonTokens: 500 }, limit: 3 },
  { id: 'e6', give: { heroChest: 1 }, cost: { moonTokens: 250 }, limit: 5 },
];
G.shopLimitBought = (store, id) => (store[id] || 0);
G.buyLimited = (item, store) => {
  if ((store[item.id] || 0) >= item.limit) return 'Sold out this week.';
  if (!G.spend(item.cost)) return 'Missing: ' + G.missing(item.cost);
  store[item.id] = (store[item.id] || 0) + 1;
  G.grant(item.give);
  return null;
};

/* ---------------- Consumables ---------------- */
G.useItem = key => {
  if (!G.state.items[key]) return null;
  G.state.items[key]--;
  const afk2 = G.afkPending(7200);
  let rew;
  switch (key) {
    case 'goldBag': rew = { gold: afk2.gold }; break;
    case 'essFlask': rew = { essence: afk2.essence }; break;
    case 'oreCrate': rew = { ore: 25 }; break;
    case 'mysteryChest': {
      const opts = [{ gems: U.randi(30, 120) }, { ore: U.randi(15, 40) }, { steel: U.randi(50, 150) }, { scrolls: 1 }, { artFrags: U.randi(3, 8) }, { tomes: U.randi(1, 4) }];
      rew = Object.assign({}, U.pick(opts), U.pick(opts), { gold: afk2.gold });
      break;
    }
    case 'heroChest': {
      const pool = HEROES.filter(h => h.rarity >= 2);
      rew = { ['shard:' + U.pick(pool).id]: 15 };
      break;
    }
  }
  G.grant(rew);
  return rew;
};

/* ---------------- Events ---------------- */
G.FESTIVAL = [
  { at: 10, reward: { ore: 20, gold: 10000 } }, { at: 30, reward: { steel: 120, gems: 50 } },
  { at: 60, reward: { scrolls: 2, artFrags: 6 } }, { at: 100, reward: { 'art:forge_soul': 6, gems: 150 } },
  { at: 200, reward: { stones: 2, gems: 300 } },
];
G.SIEGE_MAX = 30;
G.siegeEnemies = tier => {
  const base = Math.max(1, G.state.stage - 1);
  const mult = G.enemyMult(base) * (0.55 + tier * 0.07);
  const kinds = ['wraith', 'demon', 'wolf', 'skeleton', 'spider'];
  return kinds.map((k, i) => G.monsterUnit(k, mult, { boss: i === 0 && tier % 5 === 0, name: i === 0 && tier % 5 === 0 ? 'Blood Moon Herald' : undefined, col: G.chapterTint(k, 20) }));
};
G.siegeReward = tier => ({ moonTokens: 40 + tier * 8, gold: Math.round(2000 * (1 + tier * 0.3) * G.goldMult()), gems: tier % 5 === 0 ? 50 : 0 });
