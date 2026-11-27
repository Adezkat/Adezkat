/* =========================================================
   UI building blocks + all screen renderers
   ========================================================= */
'use strict';

const UI = {};

UI.stars = hi => {
  let s = '★'.repeat(hi.stars);
  if (hi.awaken) s += `<span class="aw">${'✦'.repeat(hi.awaken)}</span>`;
  return `<span class="stars">${s}</span>`;
};
UI.short = name => name.split(' ')[0];
UI.rtag = r => `<span class="tag rtext${r}">${RARITIES[r].name}</span>`;
UI.ctag = cls => `<span class="tag" style="color:${CLASSES[cls].color}">${CLASSES[cls].icon} ${CLASSES[cls].name}</span>`;
UI.statVal = (k, v) => PCT_STATS.has(k) ? `+${(Math.round(v * 10) / 10)}%` : `+${U.fmt(v)}`;
UI.dot = on => on ? '<i class="dot"></i>' : '';
UI.bar = (pct, cls = '', label = '') => `<div class="bar ${cls}"><i style="width:${U.clamp(pct, 0, 100).toFixed(1)}%"></i>${label ? `<span class="lbl">${label}</span>` : ''}</div>`;

UI.heroCard = (id, o = {}) => {
  const d = HERO_BY_ID[id], hi = G.state.heroes[id], r = d.rarity;
  const owned = !!hi, inTeam = G.state.team.includes(id);
  let bottom = '';
  if (owned) bottom = `${UI.stars(hi)}<div class="hc-name rtext${r}">${UI.short(d.name)}</div>`;
  else {
    const have = G.state.shards[id] || 0, need = RARITIES[r].unlock;
    bottom = `<div class="hc-name rtext${r} unl">${UI.short(d.name)}</div><div class="shardbar">${UI.bar(have / need * 100, 'blue')}</div><div class="tiny">${have}/${need}</div>`;
  }
  return `<div class="hcard rf${r} ${owned ? '' : 'locked'} ${o.sel ? 'sel' : ''}" data-act="${o.act || 'heroDetail'}" data-arg="${o.arg ?? id}">
    <div class="hc-top"><span>${CLASSES[d.cls].icon}</span>${owned ? `<span>Lv${hi.lvl}</span>` : '<span>🔒</span>'}</div>
    ${ART.hero(id)}
    ${owned && inTeam && !o.noTeam ? '<div class="in-team">TEAM</div>' : ''}
    ${o.badge ? `<div class="in-team" style="background:${o.badgeCol || 'var(--ok)'}">${o.badge}</div>` : ''}
    <div class="hc-bot">${bottom}</div></div>`;
};
UI.gearSlot = (it, o = {}) => {
  if (!it) return `<div class="gslot empty" ${o.act ? `data-act="${o.act}" data-arg="${o.arg}"` : ''}>${SLOT_INFO[o.slot].icon}</div>`;
  return `<div class="gslot rf${it.rarity} ${o.sel ? 'sel' : ''}" data-act="${o.act || 'itemDetail'}" data-arg="${o.arg ?? it.uid}">
    ${ART.gear(it.slot, it.rarity, it.variant)}${it.enh ? `<span class="enh">+${it.enh}</span>` : ''}<span class="il">i${it.ilvl}</span>
    ${it.owner && !o.noOwner ? `<span class="eq">${ART.hero(it.owner)}</span>` : ''}${it.locked ? '<span class="lock">🔒</span>' : it.specials.length ? '<span class="sp">✦</span>' : ''}</div>`;
};
UI.avatar = (heroId, lvl, act = '') => `<div class="avatar" ${act ? `data-act="${act}"` : ''}>${ART.hero(heroId)}${lvl ? `<span class="lvl">${lvl}</span>` : ''}</div>`;
UI.miniTeam = ids => `<div class="mini-team">${ids.map(id => `<div class="mt-h rf${HERO_BY_ID[id].rarity}">${ART.hero(id)}</div>`).join('')}</div>`;
UI.resPill = k => `<div class="res-pill"><span class="ic">${RES[k].icon}</span>${U.fmt(G.state.res[k])}</div>`;

/* ---------------- Top bar ---------------- */
UI.topbar = () => {
  const s = G.state;
  const ctx = { heroes: 'essence', forge: 'ore', equipment: 'steel', arena: 'tickets', guild: 'guildCoins', events: 'moonTokens', inventory: 'steel', campaign: 'essence', shop: 'scrolls' }[App.screen];
  const lead = s.team.find(Boolean) || 'torvald';
  return `${UI.avatar(lead, G.playerLevel(), 'openSettings')}
    <div class="pinfo"><div class="nm">${U.esc(s.name)}</div><div class="pw">⚔ ${U.fmt(G.teamPower())}</div></div>
    ${UI.resPill('gold')}${UI.resPill('gems')}${ctx ? UI.resPill(ctx) : ''}`;
};

/* ---------------- Nav ---------------- */
UI.NAV = [
  { k: 'home', i: '🏰', n: 'Home' }, { k: 'heroes', i: '🛡️', n: 'Heroes' }, { k: 'forge', i: '⚒️', n: 'Forge' },
  { k: 'campaign', i: '⚔️', n: 'Battle', c: 1 }, { k: 'arena', i: '🏟️', n: 'Arena' }, { k: 'more', i: '☰', n: 'More' },
];
UI.nav = () => UI.NAV.map(n => {
  const active = App.screen === n.k || (n.k === 'more' && ['equipment', 'inventory', 'guild', 'shop', 'events', 'more'].includes(App.screen));
  let dot = false;
  if (n.k === 'home') dot = G.missionsClaimable() || G.canClaimLogin() || G.achClaimable();
  if (n.k === 'arena') dot = G.state.res.tickets > 0 || !!G.state.arena.pendingReward;
  if (n.k === 'heroes') dot = HEROES.some(h => !G.state.heroes[h.id] && (G.state.shards[h.id] || 0) >= RARITIES[h.rarity].unlock);
  if (n.k === 'forge') dot = G.state.res.ore >= 10;
  return `<button class="nav-btn ${active ? 'active' : ''} ${n.c ? 'center' : ''}" data-act="go" data-arg="${n.k}"><span class="ni">${n.i}</span>${n.n}${UI.dot(dot)}</button>`;
}).join('');

/* =========================================================
   SCREENS
   ========================================================= */
const SCREENS = {};

/* ---------------- HOME ---------------- */
SCREENS.home = () => {
  const s = G.state, info = G.stageInfo(Math.min(s.stage, MAX_STAGE));
  const secs = G.afkSeconds(), pend = G.afkPending(secs), full = secs >= AFK_CAP_H * 3600;
  const ql = G.quickLootCost();
  const rates = G.afkRates();
  return `
  <div class="home-scene" id="home-scene-slot"></div>
  <div class="panel glow">
    <div class="afk-panel">
      <div data-act="collectAfk">${ART.chest(full).replace('class="chest-svg"', `class="chest-svg ${secs > 600 ? 'full' : ''}"`)}</div>
      <div class="grow">
        <div class="row between"><h3 style="margin:0">Idle Loot</h3><span class="small ${full ? 'bad' : 'mute'}" id="afk-time">${U.fmtTime(secs)} / ${AFK_CAP_H}h</span></div>
        ${UI.bar(secs / (AFK_CAP_H * 36), '', '')}
        <div class="mt4" id="afk-pend">${U.rewardChips({ gold: pend.gold, essence: pend.essence, ore: pend.ore, steel: pend.steel, gems: pend.gems })}</div>
        <div class="tiny mute mt4">Per hour: 🪙${U.fmt(rates.gold)} · 🌀${U.fmt(rates.essence)} · 🪨${rates.ore} · 🔩${rates.steel}</div>
      </div>
    </div>
    <div class="row gap mt">
      <button class="btn gold grow" data-act="collectAfk">Collect</button>
      <button class="btn blue grow" data-act="quickLoot">⚡ Quick Loot 2h <span class="cost">${ql ? '💎50' : 'FREE'}</span></button>
    </div>
  </div>
  ${s.stage > MAX_STAGE ? `<div class="panel center gold b">👑 All ${MAX_STAGE} stages conquered! Climb the Arena and Guild ranks.</div>` :
    `<button class="btn ember lg big-fight" data-act="stagePreview" data-arg="${s.stage}">${info.isBoss ? '💀 Boss Battle' : '⚔️ Battle'}<span class="stg">Chapter ${info.ch + 1}: ${info.chapterName} · Stage ${info.idx}/20</span></button>`}
  <div class="quick-grid mt">
    <div class="qbtn" data-act="openMissions">📋<span>Missions</span>${UI.dot(G.missionsClaimable())}</div>
    <div class="qbtn" data-act="openLogin">📅<span>Login</span>${UI.dot(G.canClaimLogin())}</div>
    <div class="qbtn" data-act="openAch">🏆<span>Trophies</span>${UI.dot(G.achClaimable())}</div>
    <div class="qbtn" data-act="go" data-arg="artifacts">🏺<span>Artifacts</span></div>
    <div class="qbtn" data-act="go" data-arg="events">🌕<span>Events</span></div>
    <div class="qbtn" data-act="go" data-arg="shop">🛒<span>Shop</span></div>
    <div class="qbtn" data-act="go" data-arg="guild">🏰<span>Guild</span></div>
    <div class="qbtn" data-act="go" data-arg="inventory">🎒<span>Bag</span></div>
  </div>`;
};

/* ---------------- MORE ---------------- */
SCREENS.more = () => `
  <div class="screen-title"><h1>Realm</h1></div>
  <div class="more-grid">
    <div class="qbtn" data-act="go" data-arg="equipment">🗡️<span>Equipment</span></div>
    <div class="qbtn" data-act="go" data-arg="inventory">🎒<span>Inventory</span></div>
    <div class="qbtn" data-act="go" data-arg="guild">🏰<span>Guild</span></div>
    <div class="qbtn" data-act="go" data-arg="shop">🛒<span>Shop</span></div>
    <div class="qbtn" data-act="go" data-arg="events">🌕<span>Events</span></div>
    <div class="qbtn" data-act="go" data-arg="artifacts">🏺<span>Artifacts</span></div>
    <div class="qbtn" data-act="openMissions">📋<span>Missions</span>${UI.dot(G.missionsClaimable())}</div>
    <div class="qbtn" data-act="openAch">🏆<span>Trophies</span>${UI.dot(G.achClaimable())}</div>
    <div class="qbtn" data-act="openSettings">⚙️<span>Settings</span></div>
  </div>
  <div class="panel mt small mute">
    <h3>Adventurer's Notes</h3>
    • Heroes fight automatically. Skills fire when the blue energy bar fills.<br>
    • Front row (first 2 slots) is targeted first — put Warriors there.<br>
    • Forge gear with Ember Ore; upgrade the Forge for better rarity odds.<br>
    • Combine 3 identical-rarity pieces of one slot into a higher rarity.<br>
    • Idle loot accumulates for up to ${AFK_CAP_H} hours, even while offline.
  </div>`;

/* ---------------- HEROES ---------------- */
SCREENS.heroes = () => {
  const s = G.state, f = App.heroFilter;
  const posName = ['Front', 'Front', 'Back', 'Back', 'Back'];
  const team = s.team.map((id, i) => `<div class="team-slot">${id ? UI.heroCard(id, { act: 'pickTeamSlot', arg: i, noTeam: true }) : `<div class="empty-slot" data-act="pickTeamSlot" data-arg="${i}">+</div>`}<div class="pos">${posName[i]}</div></div>`).join('');
  let list = HEROES.slice();
  if (f !== 'all') list = list.filter(h => h.cls === f);
  const owned = list.filter(h => s.heroes[h.id]).sort((a, b) => G.heroPower(s.heroes[b.id]) - G.heroPower(s.heroes[a.id]));
  const locked = list.filter(h => !s.heroes[h.id]).sort((a, b) => b.rarity - a.rarity);
  return `
  <div class="screen-title"><h1>Heroes</h1><div class="row gap4"><button class="btn sm" data-act="autoTeam">✨ Auto Team</button><button class="btn sm gold" data-act="go" data-arg="shop">📜 Summon</button></div></div>
  <div class="panel glow"><div class="row between mb"><h3 style="margin:0">Battle Formation</h3><span class="gold b">⚔ ${U.fmt(G.teamPower())}</span></div><div class="team-row">${team}</div></div>
  <div class="filters">${['all', ...Object.keys(CLASSES)].map(k => `<button class="fbtn ${f === k ? 'active' : ''}" data-act="heroFilter" data-arg="${k}">${k === 'all' ? 'All' : CLASSES[k].icon + ' ' + CLASSES[k].name}</button>`).join('')}</div>
  <div class="small mute mb">Owned ${G.ownedCount()}/${HEROES.length}</div>
  <div class="grid4">${owned.map(h => UI.heroCard(h.id)).join('')}${locked.map(h => UI.heroCard(h.id)).join('')}</div>`;
};

UI.heroDetail = id => {
  const s = G.state, d = HERO_BY_ID[id], hi = s.heroes[id], r = d.rarity;
  if (!hi) {
    const have = s.shards[id] || 0, need = RARITIES[r].unlock;
    return `<div class="hero-detail-top"><div class="hero-portrait rf${r}" style="filter:grayscale(.6)">${ART.hero(id)}</div>
      <div class="grow"><h2 class="rtext${r}">${d.name}</h2><div class="small mute">${d.title}</div><div class="mt4">${UI.rtag(r)} ${UI.ctag(d.cls)}</div>
      <p class="small">${CLASSES[d.cls].desc}</p></div></div>
      <div class="skill-box mt"><div class="sk-name">${d.skill.name}</div><div class="small">${d.skill.desc}</div></div>
      <div class="panel mt"><div class="row between small"><span>🧩 Shards</span><b>${have}/${need}</b></div>${UI.bar(have / need * 100, 'blue')}
      <button class="btn gold block mt ${have >= need ? '' : 'dis'}" data-act="unlockHero" data-arg="${id}">Unlock Hero</button>
      <div class="tiny mute mt4 center">Get shards from summons, shops, events and hero chests.</div></div>
      <p class="small mute"><i>${d.lore}</i></p>`;
  }
  const st = G.heroStats(hi), pw = G.statPower(st), maxL = G.maxLevel(hi);
  const lc = G.levelCost(hi.lvl), ec = G.evolveCost(hi), sc = G.skillCost(hi);
  const shards = s.shards[id] || 0;
  const inTeam = s.team.includes(id);
  const equip = SLOTS.map(sl => UI.gearSlot(hi.equip[sl] != null ? G.gearById(hi.equip[sl]) : null, { slot: sl, act: 'pickGear', arg: `${id}:${sl}`, noOwner: true })).join('');
  const specials = [...new Set(st.specials)];
  return `<div class="hero-detail-top"><div class="hero-portrait rf${r}">${ART.hero(id)}</div>
    <div class="grow" style="min-width:0"><h2 class="rtext${r}" style="font-size:18px">${d.name}</h2><div class="small mute">${d.title}</div>
    <div class="mt4">${UI.rtag(r)} ${UI.ctag(d.cls)}</div>
    <div class="mt4">${UI.stars(hi)}</div>
    <div class="small mt4">Level <b class="gold">${hi.lvl}</b>/${maxL}</div>${UI.bar(hi.lvl / maxL * 100)}
    <div class="mt4 gold b">⚔ Power ${U.fmt(pw)}</div>
    <button class="btn xs mt4 ${inTeam ? 'red' : 'green'}" data-act="toggleTeam" data-arg="${id}">${inTeam ? 'Remove from team' : 'Add to team'}</button></div></div>
  <div class="stat-list mt">
    <div><span>❤️ HP</span><b>${U.fmt(st.hp)}</b></div><div><span>⚔️ ATK</span><b>${U.fmt(st.atk)}</b></div>
    <div><span>🛡️ DEF</span><b>${U.fmt(st.def)}</b></div><div><span>💨 SPD</span><b>${st.spd}</b></div>
    <div><span>🎯 Crit</span><b>${st.crit.toFixed(1)}%</b></div><div><span>💥 Crit DMG</span><b>${Math.round(100 + st.critDmg)}%</b></div>
  </div>
  ${specials.length ? `<div class="mt4">${specials.map(k => `<span class="chip" style="color:var(--r3)" title="${SPECIALS[k].desc}">✦ ${SPECIALS[k].name}</span>`).join('')}</div>` : ''}
  <div class="grid2 mt">
    <button class="btn green ${hi.lvl >= maxL ? 'dis' : ''}" data-act="levelUp" data-arg="${id}:1">Level Up<br><span class="cost">${G.costLabel(lc)}</span></button>
    <button class="btn green ${hi.lvl >= maxL ? 'dis' : ''}" data-act="levelUp" data-arg="${id}:10">Level ×10<br><span class="cost">max affordable</span></button>
  </div>
  <div class="panel mt">
    <div class="row between"><b>⭐ Evolve</b><span class="small">🧩 ${shards}/${ec.shards}</span></div>
    <div class="tiny mute">+10% base stats and +10 max level per star (max 7★).</div>
    ${hi.stars < G.maxStars ? `<button class="btn gold block mt4 ${shards >= ec.shards ? '' : 'dis'}" data-act="evolve" data-arg="${id}">Evolve to ${hi.stars + 1}★ <span class="cost">🪙${U.fmt(ec.gold)}</span></button>` : '<div class="center gold small mt4">Max stars reached</div>'}
  </div>
  <div class="skill-box"><div class="row between"><span class="sk-name">${d.skill.name} <span class="small mute">Lv${hi.skill}/${G.maxSkill}</span></span>
    ${hi.skill < G.maxSkill ? `<button class="btn xs blue" data-act="skillUp" data-arg="${id}">Upgrade <span class="cost">${G.costLabel(sc)}</span></button>` : '<span class="small gold">MAX</span>'}</div>
    <div class="small">${d.skill.desc}</div><div class="tiny mute mt4">Skill power +${(hi.skill - 1) * 10}% from upgrades.</div></div>
  <div class="panel mt">
    <div class="row between"><b>🔮 Awakening ${'✦'.repeat(hi.awaken)}${'·'.repeat(3 - hi.awaken)}</b>
    ${hi.awaken >= 3 ? '<span class="small gold">Fully awakened</span>' : hi.stars < G.maxStars ? '<span class="small mute">Requires 7★</span>' : `<button class="btn xs ember" data-act="awaken" data-arg="${id}">Awaken <span class="cost">${G.costLabel(G.awakenCost(hi))}</span></button>`}</div>
    <div class="tiny mute">Each awakening: ${d.awaken.desc} and +10 max level.</div>
  </div>
  <div class="row between"><h3 style="margin:6px 0">Equipment</h3><div class="row gap4"><button class="btn xs" data-act="autoEquip" data-arg="${id}">Auto-equip</button><button class="btn xs ghost" data-act="unequipAll" data-arg="${id}">Unequip all</button></div></div>
  <div class="equip-row">${equip}</div>
  <p class="small mute"><i>${d.lore}</i></p>`;
};

UI.gearPicker = (heroId, slot) => {
  const hi = G.state.heroes[heroId];
  const curUid = hi.equip[slot], cur = curUid != null ? G.gearById(curUid) : null;
  const others = G.gearOf(hi).filter(g => g.slot !== slot);
  const base = G.statPower(G.heroStats(hi, cur ? others.concat([cur]) : others));
  const items = G.state.gear.filter(g => g.slot === slot && g.uid !== curUid).map(g => ({ g, p: G.statPower(G.heroStats(hi, others.concat([g]))) })).sort((a, b) => b.p - a.p);
  return `${cur ? `<div class="panel"><div class="small mute mb">Equipped</div>${UI.itemSummary(cur)}<button class="btn sm ghost block mt" data-act="unequip" data-arg="${cur.uid}">Unequip</button></div>` : '<div class="center mute small mb">Nothing equipped in this slot.</div>'}
  ${items.length ? items.map(({ g, p }) => {
    const dlt = p - base;
    return `<div class="list-row"><div style="width:52px;flex:none">${UI.gearSlot(g)}</div><div class="lr-main"><div class="t rtext${g.rarity}">${U.esc(g.name)}</div>
      <div class="tiny mute">${g.owner ? 'Worn by ' + UI.short(HERO_BY_ID[g.owner].name) + ' · ' : ''}iLv ${g.ilvl}${g.enh ? ' · +' + g.enh : ''}</div>
      <div class="small ${dlt >= 0 ? 'cmp-up' : 'cmp-down'}">${dlt >= 0 ? '▲' : '▼'} ${U.fmt(Math.abs(dlt))} power</div></div>
      <button class="btn sm gold" data-act="equipItem" data-arg="${heroId}:${g.uid}">Equip</button></div>`;
  }).join('') : `<div class="center mute small">No ${SLOT_INFO[slot].name.toLowerCase()} in your inventory. Visit the Forge!</div><button class="btn ember block mt" data-act="goForge">⚒️ Go to Forge</button>`}`;
};

UI.itemSummary = it => {
  const main = G.gearMain(it);
  return `<div class="item-card">${UI.gearSlot(it, { act: 'noop' })}<div class="grow" style="min-width:0">
    <div class="item-name rtext${it.rarity}">${U.esc(it.name)}${it.enh ? ` <span class="gold">+${it.enh}</span>` : ''}</div>
    <div class="tiny mute">${RARITIES[it.rarity].name} ${SLOT_INFO[it.slot].name} · iLv ${it.ilvl} · Score ${U.fmt(G.gearScore(it))}</div>
    ${Object.entries(main).map(([k, v]) => `<div class="affix"><span>${STAT_NAMES[k]}</span><b>${UI.statVal(k, v)}</b></div>`).join('')}
    ${it.affixes.map(a => `<div class="affix mute"><span>• ${STAT_NAMES[a.stat]}</span><span>${UI.statVal(a.stat, a.val)}</span></div>`).join('')}
    ${it.specials.map(k => `<div class="affix spc"><span>✦ ${SPECIALS[k].name}</span></div><div class="tiny" style="color:#ffd88a">${SPECIALS[k].desc}</div>`).join('')}
  </div></div>`;
};

UI.itemDetail = uid => {
  const it = G.gearById(uid);
  if (!it) return '<div class="center mute">Item no longer exists.</div>';
  const maxE = G.maxEnh(it), ec = G.enhanceCost(it), sv = G.salvageValue(it);
  return `${UI.itemSummary(it)}
    <div class="mt small">Enhancement <b class="gold">+${it.enh}</b>/${maxE}</div>${UI.bar(it.enh / maxE * 100, 'blue')}
    <div class="tiny mute mt4">+10% main stats per level. Every 5 levels boosts a random affix by 25%.</div>
    ${it.enh < maxE ? `<div class="grid2 mt"><button class="btn blue" data-act="enhance" data-arg="${uid}:1">Enhance<br><span class="cost">${G.costLabel(ec)}</span></button>
      <button class="btn blue" data-act="enhance" data-arg="${uid}:5">Enhance ×5<br><span class="cost">up to 5 levels</span></button></div>` : '<div class="center gold mt">✦ Fully enhanced ✦</div>'}
    <div class="grid3 mt">
      ${it.owner ? `<button class="btn sm" data-act="unequip" data-arg="${uid}">Unequip</button>` : `<button class="btn sm green" data-act="equipTo" data-arg="${uid}">Equip…</button>`}
      <button class="btn sm ghost" data-act="toggleLock" data-arg="${uid}">${it.locked ? '🔓 Unlock' : '🔒 Lock'}</button>
      <button class="btn sm red ${it.owner || it.locked ? 'dis' : ''}" data-act="salvageOne" data-arg="${uid}">Salvage</button>
    </div>
    <div class="tiny mute center mt4">Salvage yields ${RES.steel.icon}${sv.steel} ${RES.gold.icon}${U.fmt(sv.gold)}</div>
    ${it.owner ? `<div class="small center mt4">Worn by <b>${HERO_BY_ID[it.owner].name}</b></div>` : ''}`;
};

/* ---------------- FORGE ---------------- */
SCREENS.forge = () => {
  const t = App.forgeTab;
  const tabs = `<div class="tabs">${[['forge', '⚒️ Forge'], ['combine', '🔀 Combine'], ['salvage', '♻️ Salvage']].map(([k, n]) => `<button class="tab ${t === k ? 'active' : ''}" data-act="forgeTab" data-arg="${k}">${n}</button>`).join('')}</div>`;
  let body = '';
  const s = G.state;
  if (t === 'forge') {
    const odds = G.forgeOdds(), uc = G.forgeUpgradeCost();
    body = `<div class="panel glow">
      <div class="forge-hero" id="forge-hero"><div class="hammer">🔨</div>${ART.anvil()}<div class="sparks" id="sparks"></div></div>
      <div class="row between"><div><div class="title gold" style="font-size:17px">Forge Level ${s.forge.lvl}</div><div class="tiny mute">Item level ≈ ${G.forgeIlvl()} · scales with stage & forge level</div></div>
      ${s.forge.lvl < G.forgeMax ? `<button class="btn sm gold" data-act="upgradeForge">⬆ Upgrade<br><span class="cost">${G.costLabel(uc)}</span></button>` : '<span class="gold b">MAX</span>'}</div>
      <div class="odds mt">${odds.map((o, i) => `<div class="rf${i}" style="--rc:var(--r${i})"><div class="rtext${i}">${RARITIES[i].name.slice(0, 4)}</div>${o < 1 ? o.toFixed(2) : o.toFixed(1)}%</div>`).join('')}</div>
      <div class="row gap mt">
        <button class="btn ember grow lg" data-act="forge" data-arg="1">Forge<br><span class="cost">🪨 1</span></button>
        <button class="btn ember grow lg" data-act="forge" data-arg="10">Forge ×10<br><span class="cost">🪨 10</span></button>
      </div>
      <div class="row between mt"><span class="small">Ember Ore: <b class="gold">${U.fmt(s.res.ore)}</b></span><button class="btn xs ghost" data-act="forge" data-arg="50">Forge ×50</button></div>
    </div>
    <div class="panel small mute"><b class="gold">Forging tips</b><br>• Legendary items roll 1 special effect, Mythic roll 2.<br>• Earn ore from battles, idle loot, shops and daily missions.<br>• Unwanted items? Salvage them for Starsteel to enhance your best gear.</div>`;
  } else if (t === 'combine') {
    let rows = '';
    let any = false;
    for (let r = 0; r < 4; r++) {
      rows += `<div class="panel"><div class="row between"><h3 class="rtext${r}" style="margin:0">${RARITIES[r].name} → <span class="rtext${r + 1}">${RARITIES[r + 1].name}</span></h3></div><div class="grid5 mt">`;
      for (const sl of SLOTS) {
        const n = G.combinable(sl, r).length; if (n >= 3) any = true;
        rows += `<div class="center"><div class="gslot rf${r} ${n >= 3 ? '' : 'empty'}" style="${n >= 3 ? '' : 'filter:grayscale(.8);opacity:.6'}" data-act="combine" data-arg="${sl}:${r}">${ART.gear(sl, r)}</div><div class="tiny ${n >= 3 ? 'ok' : 'mute'}">${n}/3</div></div>`;
      }
      rows += '</div></div>';
    }
    body = `<div class="panel glow small">Combine <b>3 unequipped, unlocked</b> items of the same slot and rarity into <b>1 item of the next rarity</b> with a higher item level. Tap a slot to combine.</div>
      <button class="btn gold block mb ${any ? '' : 'dis'}" data-act="combineAll">🔀 Combine All Possible</button>${rows}`;
  } else {
    const sel = App.salvageSel;
    const cand = s.gear.filter(g => !g.owner && !g.locked && sel[g.rarity]);
    const tot = cand.reduce((a, g) => { const v = G.salvageValue(g); a.steel += v.steel; a.gold += v.gold; return a; }, { steel: 0, gold: 0 });
    body = `<div class="panel glow"><h3>Bulk Salvage</h3><div class="small mute mb">Equipped and locked items are always kept safe.</div>
      <div class="filters">${RARITIES.map((r, i) => `<button class="fbtn ${sel[i] ? 'active' : ''}" data-act="salvSel" data-arg="${i}"><span class="rtext${i}">${r.name}</span> (${s.gear.filter(g => g.rarity === i && !g.owner && !g.locked).length})</button>`).join('')}</div>
      <div class="center mt">Salvage <b>${cand.length}</b> items for ${U.rewardChips(tot)}</div>
      <button class="btn red block mt ${cand.length ? '' : 'dis'}" data-act="salvageBulk">♻️ Salvage ${cand.length} items</button></div>
      <div class="grid5">${cand.slice(0, 60).map(g => UI.gearSlot(g)).join('')}</div>`;
  }
  return `<div class="screen-title"><h1>The Forge</h1><span class="sub">Gear ${s.gear.length}</span></div>${tabs}${body}`;
};

/* ---------------- EQUIPMENT ---------------- */
SCREENS.equipment = () => {
  const s = G.state;
  const team = s.team.filter(Boolean);
  if (!App.eqHero || !s.heroes[App.eqHero]) App.eqHero = team[0] || Object.keys(s.heroes)[0];
  const hid = App.eqHero, hi = s.heroes[hid], d = HERO_BY_ID[hid];
  const st = G.heroStats(hi);
  const slotF = App.eqSlot;
  const others = G.gearOf(hi).filter(g => g.slot !== slotF);
  const curUid = hi.equip[slotF];
  const base = G.statPower(st);
  const items = s.gear.filter(g => g.slot === slotF).map(g => ({ g, p: G.statPower(G.heroStats(hi, others.concat([g]))) })).sort((a, b) => b.p - a.p).slice(0, 40);
  const heroTabs = Object.keys(s.heroes).sort((a, b) => (team.includes(b) - team.includes(a)) || G.heroPower(s.heroes[b]) - G.heroPower(s.heroes[a]));
  return `<div class="screen-title"><h1>Equipment</h1><button class="btn sm gold" data-act="autoEquipTeam">✨ Auto-equip Team</button></div>
  <div class="filters">${heroTabs.map(id => `<div style="width:54px;flex:none">${UI.heroCard(id, { act: 'eqHero', arg: id, sel: id === hid })}</div>`).join('')}</div>
  <div class="panel glow">
    <div class="row between"><div><b class="rtext${d.rarity}">${d.name}</b> <span class="small mute">Lv${hi.lvl}</span></div><span class="gold b">⚔ ${U.fmt(base)}</span></div>
    <div class="equip-row mt">${SLOTS.map(sl => `<div>${UI.gearSlot(hi.equip[sl] != null ? G.gearById(hi.equip[sl]) : null, { slot: sl, act: hi.equip[sl] != null ? 'itemDetail' : 'eqSlot', arg: hi.equip[sl] != null ? hi.equip[sl] : sl, noOwner: true, sel: sl === slotF })}<div class="tiny center mute">${SLOT_INFO[sl].name}</div></div>`).join('')}</div>
    <div class="stat-list mt"><div><span>❤️ HP</span><b>${U.fmt(st.hp)}</b></div><div><span>⚔️ ATK</span><b>${U.fmt(st.atk)}</b></div><div><span>🛡️ DEF</span><b>${U.fmt(st.def)}</b></div><div><span>💨 SPD</span><b>${st.spd}</b></div></div>
    <div class="row gap mt"><button class="btn sm grow" data-act="autoEquip" data-arg="${hid}">Auto-equip</button><button class="btn sm grow ghost" data-act="unequipAll" data-arg="${hid}">Unequip all</button><button class="btn sm grow ghost" data-act="heroDetail" data-arg="${hid}">Hero info</button></div>
  </div>
  <div class="filters">${SLOTS.map(sl => `<button class="fbtn ${sl === slotF ? 'active' : ''}" data-act="eqSlot" data-arg="${sl}">${SLOT_INFO[sl].icon} ${SLOT_INFO[sl].name}</button>`).join('')}</div>
  <div class="panel">${items.length ? items.map(({ g, p }) => {
    const dl = p - base, isCur = g.uid === curUid;
    return `<div class="list-row"><div style="width:50px;flex:none">${UI.gearSlot(g)}</div><div class="lr-main"><div class="t rtext${g.rarity}">${U.esc(g.name)}${g.enh ? ' +' + g.enh : ''}</div>
      <div class="tiny mute">iLv ${g.ilvl} · ${g.owner ? (isCur ? 'Equipped' : 'On ' + UI.short(HERO_BY_ID[g.owner].name)) : 'In bag'}${g.specials.length ? ' · ✦' + g.specials.map(k => SPECIALS[k].name).join(', ') : ''}</div>
      ${isCur ? '' : `<div class="small ${dl >= 0 ? 'cmp-up' : 'cmp-down'}">${dl >= 0 ? '▲' : '▼'} ${U.fmt(Math.abs(dl))}</div>`}</div>
      ${isCur ? '<span class="tag gold">Worn</span>' : `<button class="btn sm gold" data-act="equipItem" data-arg="${hid}:${g.uid}">Equip</button>`}</div>`;
  }).join('') : `<div class="center mute small">No ${SLOT_INFO[slotF].name} yet.</div><button class="btn ember block mt" data-act="goForge">⚒️ Forge some</button>`}</div>`;
};

/* ---------------- INVENTORY ---------------- */
SCREENS.inventory = () => {
  const s = G.state, t = App.invTab;
  const tabs = `<div class="tabs">${[['mats', 'Materials'], ['gear', 'Gear'], ['chests', 'Chests'], ['shards', 'Shards']].map(([k, n]) => `<button class="tab ${t === k ? 'active' : ''}" data-act="invTab" data-arg="${k}">${n}${k === 'chests' ? UI.dot(Object.values(s.items).some(v => v > 0)) : ''}</button>`).join('')}</div>`;
  let body = '';
  if (t === 'mats') {
    body = `<div class="mat-grid">${Object.keys(RES).map(k => `<div class="mat" data-act="resInfo" data-arg="${k}"><div class="mi">${RES[k].icon}</div><div class="mq">${U.fmt(s.res[k])}</div><div class="mn">${RES[k].name}</div></div>`).join('')}</div>`;
  } else if (t === 'gear') {
    const sf = App.invSlot;
    let list = s.gear.slice();
    if (sf !== 'all') list = list.filter(g => g.slot === sf);
    list.sort((a, b) => b.rarity - a.rarity || G.gearScore(b) - G.gearScore(a));
    body = `<div class="filters">${['all', ...SLOTS].map(k => `<button class="fbtn ${sf === k ? 'active' : ''}" data-act="invSlot" data-arg="${k}">${k === 'all' ? 'All' : SLOT_INFO[k].icon + ' ' + SLOT_INFO[k].name}</button>`).join('')}</div>
      <div class="small mute mb">${list.length} items · tap for details</div>
      <div class="grid5">${list.slice(0, 150).map(g => UI.gearSlot(g)).join('')}</div>${list.length > 150 ? '<div class="center small mute mt">Showing top 150 — salvage extras at the Forge.</div>' : ''}
      ${!list.length ? '<button class="btn ember block" data-act="goForge">⚒️ Visit the Forge</button>' : ''}`;
  } else if (t === 'chests') {
    body = `<div class="panel">${Object.keys(CONSUMABLES).map(k => `<div class="list-row"><div class="lr-ic">${CONSUMABLES[k].icon}</div><div class="lr-main"><div class="t">${CONSUMABLES[k].name} <span class="gold">×${s.items[k] || 0}</span></div><div class="tiny mute">${CONSUMABLES[k].desc}</div></div>
      <div class="col gap4"><button class="btn xs gold ${s.items[k] ? '' : 'dis'}" data-act="useItem" data-arg="${k}:1">Use</button><button class="btn xs ${s.items[k] > 1 ? '' : 'dis'}" data-act="useItem" data-arg="${k}:all">All</button></div></div>`).join('')}
      <div class="list-row"><div class="lr-ic">📜</div><div class="lr-main"><div class="t">Summon Scrolls <span class="gold">×${s.res.scrolls}</span></div><div class="tiny mute">Use at the Tavern to summon heroes.</div></div><button class="btn xs gold" data-act="goShopTab" data-arg="tavern">Tavern</button></div></div>`;
  } else {
    const list = HEROES.filter(h => (s.shards[h.id] || 0) > 0);
    body = list.length ? `<div class="panel">${list.map(h => {
      const have = s.shards[h.id], owned = !!s.heroes[h.id];
      const need = owned ? (s.heroes[h.id].stars >= G.maxStars ? 0 : G.evolveCost(s.heroes[h.id]).shards) : RARITIES[h.rarity].unlock;
      return `<div class="list-row" data-act="heroDetail" data-arg="${h.id}"><div style="width:44px;flex:none">${UI.heroCard(h.id, { act: 'heroDetail' })}</div><div class="lr-main"><div class="t rtext${h.rarity}">${h.name}</div>
        <div class="tiny mute">${owned ? 'Evolve' : 'Unlock'}: ${have}/${need || '—'}</div>${need ? UI.bar(have / need * 100, 'blue') : ''}</div>
        ${need && have >= need ? '<span class="tag ok">READY</span>' : ''}</div>`;
    }).join('')}</div>` : '<div class="panel center mute">No hero shards yet. Summon duplicates or buy shards in shops.</div>';
  }
  return `<div class="screen-title"><h1>Inventory</h1></div>${tabs}${body}`;
};

/* ---------------- CAMPAIGN ---------------- */
SCREENS.campaign = () => {
  const s = G.state;
  const curCh = Math.floor((Math.min(s.stage, MAX_STAGE) - 1) / STAGES_PER_CHAPTER);
  if (App.chView == null || App.chView > curCh) App.chView = curCh;
  const ch = App.chView;
  let nodes = '';
  for (let i = 1; i <= STAGES_PER_CHAPTER; i++) {
    const n = ch * STAGES_PER_CHAPTER + i;
    const info = G.stageInfo(n);
    const done = n < s.stage, cur = n === s.stage;
    nodes += `<div class="snode ${done ? 'done' : ''} ${cur ? 'cur' : ''} ${!done && !cur ? 'lock' : ''} ${info.isBoss ? 'boss' : ''}" data-act="${cur ? 'stagePreview' : done ? 'stageInfo' : 'noop'}" data-arg="${n}">${info.isBoss ? '💀' : i}<small>${info.isBoss ? 'BOSS' : n}</small></div>`;
  }
  const cleared = Math.max(0, Math.min(STAGES_PER_CHAPTER, s.stage - 1 - ch * STAGES_PER_CHAPTER));
  return `<div class="screen-title"><h1>Campaign</h1><span class="sub">Stage ${Math.min(s.stage, MAX_STAGE)}/${MAX_STAGE}</span></div>
  <div class="chapter-head"><div class="bstage theme-${ch % 6}"><div class="bg-sky"></div><div class="bg-hills"></div><div class="bg-ground"></div><div class="bg-fog"></div></div>
    <div class="ch-txt"><div class="small mute b">CHAPTER ${ch + 1}</div><h2>${CHAPTERS[ch]}</h2><div class="small">${cleared}/${STAGES_PER_CHAPTER} cleared · Boss: ${BOSS_NAMES[ch]}</div></div>
    <div class="ch-nav"><button class="btn sm ${ch > 0 ? '' : 'dis'}" data-act="chNav" data-arg="-1">◀</button><button class="btn sm ${ch < curCh ? '' : 'dis'}" data-act="chNav" data-arg="1">▶</button></div></div>
  <div class="panel"><div class="stage-path">${nodes}</div></div>
  <div class="panel row between"><div><b>Auto-Push</b><div class="tiny mute">Automatically start the next stage after a victory.</div></div>
    <button class="btn sm ${s.settings.autoPush ? 'green' : 'ghost'}" data-act="toggleAutoPush">${s.settings.autoPush ? 'ON' : 'OFF'}</button></div>
  <div class="panel small mute">Enemies grow stronger with every stage. Bosses appear on every 10th stage and drop gems, Relic Dust and guaranteed gear. Chapter bosses (stage 20) also drop Summon Scrolls.</div>`;
};
UI.stagePreview = n => {
  const info = G.stageInfo(n), en = G.stageEnemies(n), ep = G.unitsPower(en), tp = G.teamPower();
  const rw = G.stageRewards(n);
  const ratio = tp / ep;
  const verdict = ratio > 1.3 ? ['Easy', 'ok'] : ratio > 0.95 ? ['Even', 'gold'] : ratio > 0.75 ? ['Hard', 'bad'] : ['Deadly', 'bad'];
  return `<div class="center small mute">Chapter ${info.ch + 1} · ${info.chapterName}</div>
    <h2 class="center ${info.isBoss ? 'rtext4' : 'gold'}" style="margin:4px 0 10px">${info.isBoss ? '💀 ' + info.bossName : 'Stage ' + info.idx}</h2>
    <div class="enemy-row">${en.map(u => u ? `<div class="mcard ${u.isBoss ? 'boss' : ''}">${u.sprite}</div>` : '<div></div>').join('')}</div>
    <div class="panel mt"><div class="pw-compare"><span class="ok">⚔ ${U.fmt(tp)}</span><span class="${verdict[1]}">${verdict[0]}</span><span class="bad">${U.fmt(ep)} ☠</span></div>
    ${UI.bar(tp / (tp + ep) * 100, 'green')}</div>
    <div class="small b">First-clear rewards</div><div class="mt4">${U.rewardChips(rw)}${info.isBoss ? '<span class="chip">🗡️ Gear drop</span>' : '<span class="chip">🗡️ 35% gear</span>'}</div>
    <button class="btn ember lg block mt" data-act="fightStage" data-arg="${n}">⚔️ Fight!</button>`;
};

/* ---------------- ARENA ---------------- */
SCREENS.arena = () => {
  const s = G.state, a = s.arena, t = App.arenaTab;
  const tier = G.arenaTier(a.rating);
  const lb = G.leaderboard(); const myRank = lb.findIndex(x => x.me) + 1;
  const tabs = `<div class="tabs">${[['battle', '⚔️ Battle'], ['rank', '🏆 Rankings'], ['rewards', '🎁 Rewards'], ['log', '📜 Log']].map(([k, n]) => `<button class="tab ${t === k ? 'active' : ''}" data-act="arenaTab" data-arg="${k}">${n}${k === 'rewards' ? UI.dot(!!a.pendingReward) : ''}</button>`).join('')}</div>`;
  let body = '';
  if (t === 'battle') {
    if (!a.opps.length) G.genOpponents();
    body = `<div class="panel arena-hero glow"><div class="tier-badge" style="--tc:${tier.col}">🏟️</div>
      <h2 style="color:${tier.col}">${tier.name}</h2><div class="b" style="font-size:20px">${a.rating} <span class="small mute">rating</span></div>
      <div class="small mute">Rank #${myRank} · ${a.wins}W / ${a.losses}L · Best ${a.best}</div>
      <div class="row gap mt" style="justify-content:center"><span class="chip">🎟️ ${s.res.tickets} tickets</span><button class="btn xs" data-act="buyTicket">+1 🎟️ <span class="cost">💎50</span></button></div>
      <div class="tiny mute mt4">Weekly rewards in ${U.fmtTime(U.secsToNextWeek())} · Tickets refill to 5 daily</div></div>
      <div class="row between mb"><h3 style="margin:0" class="gold">Challengers</h3><button class="btn xs" data-act="refreshOpps">🔄 Refresh</button></div>
      ${a.opps.map((o, i) => {
        const team = G.virtualTeam(o.seed, o.power);
        return `<div class="panel opp ${o.done ? 'dis' : ''}" style="${o.done ? 'opacity:.5' : ''}">${UI.avatar(o.avatar)}<div class="grow" style="min-width:0"><div class="b">${U.esc(o.name)}</div>
          <div class="tiny mute">Rating ${o.rating} · ⚔ ${U.fmt(o.power)}</div>${UI.miniTeam(team.map(u => u.heroId))}</div>
          ${o.done ? '<span class="tag mute">Done</span>' : `<button class="btn sm ember" data-act="arenaFight" data-arg="${i}">Fight<br><span class="cost">🎟️1</span></button>`}</div>`;
      }).join('')}
      <div class="small mute center">Your defense team is your current battle formation.</div>`;
  } else if (t === 'rank') {
    body = `<div class="panel">${lb.map((x, i) => `<div class="lb-row ${x.me ? 'me' : ''}"><div class="rk ${i < 3 ? 'top' : ''}">${['🥇', '🥈', '🥉'][i] || i + 1}</div>${UI.avatar(x.avatar)}
      <div class="grow" style="min-width:0"><div class="b" style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${U.esc(x.name)}${x.me ? ' (You)' : ''}</div><div class="tiny mute">⚔ ${U.fmt(x.power)}</div></div>
      <div class="b" style="color:${G.arenaTier(x.rating).col}">${x.rating}</div></div>`).join('')}</div>`;
  } else if (t === 'rewards') {
    body = `${a.pendingReward ? `<div class="panel glow center"><h3>Last Week: ${a.pendingReward.tier}</h3><div>${U.rewardChips({ gems: a.pendingReward.gems, arenaCoins: a.pendingReward.arenaCoins, scrolls: a.pendingReward.scrolls })}</div>
      <button class="btn gold block mt" data-act="claimArenaWeekly">Claim Weekly Rewards</button></div>` : ''}
      <div class="panel"><h3>Weekly Tier Rewards</h3><div class="tiny mute mb">Granted every Monday based on your rating. Ratings soft-reset each week.</div>
      ${G.ARENA_TIERS.map(x => `<div class="list-row"><div class="lr-ic" style="color:${x.col}">🏅</div><div class="lr-main"><div class="t" style="color:${x.col}">${x.name}</div><div class="tiny mute">${x.min}+ rating</div></div><div style="text-align:right">${U.rewardChips({ gems: x.gems, arenaCoins: x.coins, scrolls: x.scrolls })}</div></div>`).join('')}</div>
      <button class="btn block" data-act="goShopTab" data-arg="arena">🏅 Open Arena Shop</button>`;
  } else {
    body = `<div class="panel">${a.log.length ? a.log.map(l => `<div class="list-row"><div class="lr-ic">${l.win ? '🏆' : '💀'}</div><div class="lr-main"><div class="t">${l.win ? 'Victory' : 'Defeat'} vs ${U.esc(l.name)}</div><div class="tiny mute">${new Date(l.t).toLocaleString()}</div></div><b class="${l.delta >= 0 ? 'ok' : 'bad'}">${l.delta >= 0 ? '+' : ''}${l.delta}</b></div>`).join('') : '<div class="center mute">No battles yet.</div>'}</div>`;
  }
  return `<div class="screen-title"><h1>Arena</h1><span class="sub">🏅 ${U.fmt(s.res.arenaCoins)}</span></div>${tabs}${body}`;
};

/* ---------------- GUILD ---------------- */
SCREENS.guild = () => {
  const s = G.state, gd = s.guild;
  if (!gd) {
    return `<div class="screen-title"><h1>Guilds</h1></div>
    ${App.guildCreate ? `<div class="panel glow"><h3>Found a Guild</h3><input class="txt-in" id="gname" maxlength="24" placeholder="Guild name" value="${U.esc(App.guildName || '')}">
      <div class="icon-pick mt">${['⚔️', '🐉', '🦅', '🔥', '🌙', '👑', '🗡️', '🐺', '💀', '🌹'].map(ic => `<button class="${App.guildIcon === ic ? 'active' : ''}" data-act="guildIcon" data-arg="${ic}">${ic}</button>`).join('')}</div>
      <div class="row gap mt"><button class="btn ghost grow" data-act="guildCreateToggle">Cancel</button><button class="btn gold grow" data-act="createGuild">Create <span class="cost">💎300</span></button></div></div>` :
      `<div class="panel glow row between"><div><b>Lead your own guild</b><div class="tiny mute">Found a guild and recruit allies.</div></div><button class="btn gold sm" data-act="guildCreateToggle">Create</button></div>`}
    <h3 class="gold mb">Recruiting Guilds</h3>
    ${G.guildCatalog().map(c => `<div class="panel row gap"><div class="guild-emblem" style="width:50px;height:50px;font-size:26px">${c.icon}</div><div class="grow" style="min-width:0"><div class="b">${c.name}</div>
      <div class="tiny mute">Lv ${c.lvl} · ${c.members}/30 members · ⚔ ${U.fmt(c.power)}</div><div class="tiny ${G.teamPower() >= c.req ? 'ok' : 'bad'}">Requires ⚔ ${U.fmt(c.req)}</div></div>
      <button class="btn sm green" data-act="joinGuild" data-arg="${c.id}">Join</button></div>`).join('')}`;
  }
  const t = App.guildTab;
  const need = G.guildXpNeed(gd.lvl);
  const tabs = `<div class="tabs">${[['boss', '🐉 Boss'], ['donate', '💰 Donate'], ['war', '⚔️ War'], ['members', '👥 Members'], ['rank', '🏆 Rank']].map(([k, n]) => `<button class="tab ${t === k ? 'active' : ''}" data-act="guildTab" data-arg="${k}">${n}${k === 'boss' ? UI.dot(gd.attempts > 0) : k === 'donate' ? UI.dot(gd.donations < 3) : k === 'war' ? UI.dot(gd.warAttacks > 0 || !!gd.pendingWar) : ''}</button>`).join('')}</div>`;
  let body = '';
  if (t === 'boss') {
    const bu = G.guildBossUnit();
    body = `<div class="panel glow"><div class="boss-art">${bu.sprite}</div>
      <div class="row between mt"><h3 style="margin:0" class="rtext4">${G.guildBossName()}</h3><span class="tag gold">Lv ${gd.bossLvl}</span></div>
      <div class="bar red tall mt4"><i style="width:${(gd.bossHp / gd.bossMax * 100).toFixed(1)}%"></i><span class="lbl">${U.fmt(gd.bossHp)} / ${U.fmt(gd.bossMax)}</span></div>
      <div class="row between small mt4"><span>Attempts today: <b class="gold">${gd.attempts}/3</b></span><span class="mute">Best hit ${U.fmt(gd.myBestDmg)}</span></div>
      <button class="btn ember lg block mt ${gd.attempts > 0 ? '' : 'dis'}" data-act="guildBossFight">⚔️ Challenge (30s)</button>
      <div class="tiny mute mt4">Deal as much damage as you can in 30 seconds. All guildmates share the boss HP pool. Slaying it grants everyone gems, Guild Seals and Relic Dust.</div></div>`;
  } else if (t === 'donate') {
    body = `<div class="panel glow"><h3>Donations <span class="small mute">${gd.donations}/3 today</span></h3>
      ${G.DONATIONS.map((d, i) => `<div class="list-row"><div class="lr-ic">${['🪙', '💎', '🪨'][i]}</div><div class="lr-main"><div class="t">${d.label}</div><div class="tiny mute">+${d.xp} guild XP · +${d.coins} 🛡️ Seals</div></div>
        <button class="btn sm gold ${gd.donations < 3 ? '' : 'dis'}" data-act="donate" data-arg="${i}">${G.costLabel(d.cost())}</button></div>`).join('')}</div>
      <div class="panel small"><b class="gold">Guild Perk</b><br>Each guild level grants every hero <b>+1% ATK and HP</b>. Current bonus: <b class="ok">+${gd.lvl}%</b></div>
      <button class="btn block" data-act="goShopTab" data-arg="guild">🛡️ Open Guild Shop</button>`;
  } else if (t === 'war') {
    const w = G.ensureWar();
    body = `${gd.pendingWar ? `<div class="panel glow center"><h3>${gd.pendingWar.win ? '🏆 War Won!' : '💀 War Lost'}</h3><div class="small">vs ${gd.pendingWar.enemy}: ${gd.pendingWar.ours} ★ to ${gd.pendingWar.theirs} ★</div>
      <button class="btn gold block mt" data-act="claimWar">Claim ${gd.pendingWar.win ? '600' : '200'} 🛡️ + ${gd.pendingWar.win ? '300' : '80'} 💎</button></div>` : ''}
      <div class="panel glow"><div class="row between"><div class="center grow"><div class="guild-emblem" style="margin:0 auto">${gd.icon}</div><div class="b small mt4">${U.esc(gd.name)}</div><div class="gold b" style="font-size:22px">${w.myStars + w.allyStars} ★</div></div>
        <div class="title" style="font-size:22px;color:var(--blood)">VS</div>
        <div class="center grow"><div class="guild-emblem" style="margin:0 auto;border-color:var(--blood)">${w.enemyIcon}</div><div class="b small mt4">${w.enemy}</div><div class="bad b" style="font-size:22px">${w.enemyStars} ★</div></div></div>
        <div class="tiny mute center mt">War ends in ${U.fmtTime(U.secsToNextWeek())} · Your attacks today: <b class="gold">${gd.warAttacks}/2</b> · Your stars: ${w.myStars}</div></div>
      ${w.defenders.map((d, i) => { const team = G.virtualTeam(d.seed, d.power); return `<div class="panel opp">${UI.avatar(team[0].heroId)}<div class="grow"><div class="b">${U.esc(d.name)} <span class="tiny mute">Defender ${i + 1}</span></div><div class="tiny mute">⚔ ${U.fmt(d.power)}</div>${UI.miniTeam(team.map(u => u.heroId))}
        <div class="stars">${'★'.repeat(d.stars)}<span class="mute">${'☆'.repeat(3 - d.stars)}</span></div></div>
        <button class="btn sm ember ${gd.warAttacks > 0 && d.stars < 3 ? '' : 'dis'}" data-act="warFight" data-arg="${i}">Attack</button></div>`; }).join('')}
      <div class="tiny mute center">Earn up to 3 stars per defender — more surviving heroes, more stars.</div>`;
  } else if (t === 'members') {
    const me = { name: s.name + ' (You)', role: gd.members.length ? 'Member' : 'Leader', power: G.teamPower(), contrib: gd.myContrib, avatar: s.team.find(Boolean) || 'torvald', me: true };
    const all = [me, ...gd.members].sort((a, b) => b.contrib - a.contrib);
    body = `<div class="panel">${all.map(m => `<div class="member" style="${m.me ? 'background:rgba(233,180,76,.12);border-radius:8px' : ''}">${UI.avatar(m.avatar)}<div class="grow" style="min-width:0"><div class="b">${U.esc(m.name)}</div><div class="tiny mute">${m.role} · ⚔ ${U.fmt(m.power)}</div></div><div class="small gold">${U.fmt(m.contrib)} 🎖️</div></div>`).join('')}</div>
      <button class="btn red block" data-act="leaveGuild">Leave Guild</button>`;
  } else {
    body = `<div class="panel">${G.guildRanking().map((x, i) => `<div class="lb-row ${x.me ? 'me' : ''}"><div class="rk ${i < 3 ? 'top' : ''}">${['🥇', '🥈', '🥉'][i] || i + 1}</div><div class="guild-emblem" style="width:32px;height:32px;font-size:16px;border-radius:8px">${x.icon}</div>
      <div class="grow"><div class="b">${U.esc(x.name)}</div><div class="tiny mute">Lv ${x.lvl}</div></div><div class="b gold">⚔ ${U.fmt(x.power)}</div></div>`).join('')}</div>
      <div class="small mute center">Ranking by total member power. Top guilds earn prestige — keep growing!</div>`;
  }
  return `<div class="screen-title"><h1>Guild</h1><span class="sub">🛡️ ${U.fmt(s.res.guildCoins)}</span></div>
  <div class="panel glow row gap"><div class="guild-emblem">${gd.icon}</div><div class="grow" style="min-width:0"><h3 style="margin:0">${U.esc(gd.name)}</h3>
    <div class="tiny mute">Level ${gd.lvl} · ${gd.members.length + 1} members · Perk +${gd.lvl}% ATK/HP</div>${UI.bar(gd.xp / need * 100, 'blue', `${gd.xp}/${need} XP`)}</div></div>
  ${tabs}${body}`;
};

/* ---------------- SHOP ---------------- */
SCREENS.shop = () => {
  const s = G.state, t = App.shopTab;
  const tabs = `<div class="tabs">${[['tavern', '📜 Tavern'], ['market', '🛒 Market'], ['arena', '🏅 Arena'], ['guild', '🛡️ Guild']].map(([k, n]) => `<button class="tab ${t === k ? 'active' : ''}" data-act="shopTab" data-arg="${k}">${n}${k === 'market' ? UI.dot(s.shop.freeGems !== s.day || s.shop.freeGold !== s.day) : ''}</button>`).join('')}</div>`;
  let body = '';
  const itemIcon = give => {
    const k = Object.keys(give)[0];
    if (RES[k]) return RES[k].icon; if (CONSUMABLES[k]) return CONSUMABLES[k].icon;
    if (k.startsWith('shard:')) return `<div style="width:40px;height:44px">${ART.hero(k.slice(6))}</div>`;
    if (k.startsWith('art:')) return ART_BY_ID[k.slice(4)].icon;
    return '🎁';
  };
  const itemName = give => U.rewardChips(give, 'tiny');
  if (t === 'tavern') {
    body = `<div class="summon-hero"><div class="portal"></div><h2 class="gold">The Wayfarer's Tavern</h2><div class="small mute">Call heroes from across the realm.</div>
      <div class="row gap mt" style="justify-content:center"><span class="chip">📜 ${s.res.scrolls}</span><span class="chip">💎 ${U.fmt(s.res.gems)}</span></div>
      <div class="grid2 mt">
        <button class="btn blue" data-act="summon" data-arg="1">Summon ×1<br><span class="cost">${s.res.scrolls >= 1 ? '📜 1' : '💎 150'}</span></button>
        <button class="btn gold" data-act="summon" data-arg="10">Summon ×10<br><span class="cost">${s.res.scrolls >= 10 ? '📜 10' : '💎 1,350'}</span></button>
      </div>
      <div class="tiny mute mt">Legendary+ guaranteed within <b class="gold">${60 - s.pity}</b> summons · ×10 guarantees an Epic+</div></div>
      <div class="panel mt"><h3>Summon Rates</h3><div class="odds">${G.SUMMON_RATES.map((o, i) => `<div style="--rc:var(--r${i})"><div class="rtext${i}">${RARITIES[i].name.slice(0, 4)}</div>${o}%</div>`).join('')}</div>
      <div class="tiny mute mt">Duplicate heroes become shards used to evolve stars.</div></div>`;
  } else if (t === 'market') {
    const deals = G.ensureDeals();
    body = `<div class="grid2 mb">
      <div class="shop-item"><div class="si-ic">💎</div><div class="si-nm">Daily Gem Cache<br>50 Gems</div><button class="btn sm green block ${s.shop.freeGems === s.day ? 'dis' : ''}" data-act="freeGems">${s.shop.freeGems === s.day ? 'Claimed' : 'FREE'}</button></div>
      <div class="shop-item"><div class="si-ic">💰</div><div class="si-nm">Daily Gold Chest<br>1h of idle gold</div><button class="btn sm green block ${s.shop.freeGold === s.day ? 'dis' : ''}" data-act="freeGold">${s.shop.freeGold === s.day ? 'Claimed' : 'FREE'}</button></div></div>
      <div class="row between mb"><h3 style="margin:0" class="gold">Daily Deals</h3><span class="tiny mute">Refresh in ${U.fmtTime(U.secsToNextDay())}</span></div>
      <div class="grid3">${deals.map((d, i) => { const sold = s.shop.bought[i]; const c = G.dealCost(d); return `<div class="shop-item ${sold ? 'sold' : ''}">${d.off ? `<span class="off">-${d.off}%</span>` : ''}<div class="si-ic">${itemIcon(d.give)}</div><div class="si-nm">${itemName(d.give)}</div>
        <button class="btn xs gold block ${sold ? 'dis' : ''}" data-act="buyDeal" data-arg="${i}">${sold ? 'Sold' : G.costLabel(c)}</button></div>`; }).join('')}</div>
      <div class="panel mt"><h3>Exchange</h3>
        <div class="list-row"><div class="lr-ic">🪨</div><div class="lr-main"><div class="t">10 Ember Ore</div></div><button class="btn sm" data-act="exchange" data-arg="ore">💎 40</button></div>
        <div class="list-row"><div class="lr-ic">🔩</div><div class="lr-main"><div class="t">50 Starsteel</div></div><button class="btn sm" data-act="exchange" data-arg="steel">💎 40</button></div>
        <div class="list-row"><div class="lr-ic">🌀</div><div class="lr-main"><div class="t">4h of Spirit Essence</div></div><button class="btn sm" data-act="exchange" data-arg="essence">💎 60</button></div>
        <div class="list-row"><div class="lr-ic">📘</div><div class="lr-main"><div class="t">1 Skill Tome</div></div><button class="btn sm" data-act="exchange" data-arg="tomes">💎 30</button></div></div>`;
  } else {
    const isArena = t === 'arena';
    const list = isArena ? G.ARENA_SHOP : G.GUILD_SHOP;
    const store = isArena ? s.shop.arenaBought : s.shop.guildBought;
    const cur = isArena ? 'arenaCoins' : 'guildCoins';
    body = `<div class="panel glow row between"><span>${RES[cur].name}</span><b class="gold">${RES[cur].icon} ${U.fmt(s.res[cur])}</b></div>
      ${!isArena && !s.guild ? '<div class="panel small mute center">Join a guild to earn Guild Seals.</div>' : ''}
      <div class="grid3">${list.map(it => { const b = store[it.id] || 0, sold = b >= it.limit; return `<div class="shop-item ${sold ? 'sold' : ''}"><span class="lim">${b}/${it.limit}</span><div class="si-ic">${itemIcon(it.give)}</div><div class="si-nm">${itemName(it.give)}</div>
        <button class="btn xs gold block ${sold ? 'dis' : ''}" data-act="buyLimited" data-arg="${t}:${it.id}">${sold ? 'Sold out' : G.costLabel(it.cost)}</button></div>`; }).join('')}</div>
      <div class="tiny mute center mt">Limits reset weekly.</div>`;
  }
  return `<div class="screen-title"><h1>Shop</h1></div>${tabs}${body}`;
};

/* ---------------- EVENTS ---------------- */
SCREENS.events = () => {
  const s = G.state, ev = s.events;
  const tier = ev.siege;
  let track = '';
  for (let i = 1; i <= G.SIEGE_MAX; i++) track += `<div class="tnode ${i < tier ? 'done' : ''} ${i === tier ? 'cur' : ''} ${i % 5 === 0 ? 'boss' : ''}">${i % 5 === 0 ? '🌑' : i}<small>T${i}</small></div>`;
  const en = tier <= G.SIEGE_MAX ? G.siegeEnemies(tier) : null;
  return `<div class="screen-title"><h1>Events</h1><span class="sub">🌕 ${U.fmt(s.res.moonTokens)}</span></div>
  <div class="event-banner moon"><span class="timer">⏳ Ends in ${U.fmtTime(U.secsToNextWeek())}</span><h2>Blood Moon Siege</h2><div class="small">Hold the walls against 30 escalating waves of moon-maddened horrors.</div></div>
  <div class="panel glow">
    <div class="tier-track" id="tier-track">${track}</div>
    ${en ? `<div class="enemy-row">${en.map(u => `<div class="mcard ${u.isBoss ? 'boss' : ''}">${u.sprite}</div>`).join('')}</div>
    <div class="row between mt"><span class="small">Tier ${tier} · ☠ ${U.fmt(G.unitsPower(en))}</span><span>${U.rewardChips(G.siegeReward(tier))}</span></div>
    <button class="btn red lg block mt" data-act="siegeFight">🌑 Assault Tier ${tier}</button>` : '<div class="center gold b">All tiers conquered this week! 🌕</div>'}
  </div>
  <div class="panel"><h3>🌕 Moon Exchange</h3><div class="grid3">${G.EVENT_SHOP.map(it => { const b = ev.shopBought[it.id] || 0, sold = b >= it.limit; const k = Object.keys(it.give)[0];
    const ic = k.startsWith('shard:') ? `<div style="width:40px;height:44px">${ART.hero(k.slice(6))}</div>` : k.startsWith('art:') ? ART_BY_ID[k.slice(4)].icon : RES[k] ? RES[k].icon : CONSUMABLES[k].icon;
    return `<div class="shop-item ${sold ? 'sold' : ''}"><span class="lim">${b}/${it.limit}</span><div class="si-ic">${ic}</div><div class="si-nm">${U.rewardChips(it.give, 'tiny')}</div><button class="btn xs gold block ${sold ? 'dis' : ''}" data-act="buyEvent" data-arg="${it.id}">${sold ? 'Sold out' : G.costLabel(it.cost)}</button></div>`; }).join('')}</div>
    <div class="tiny mute center mt">Collect Valka Soulreaver shards — a Mythic assassin!</div></div>
  <div class="event-banner fest"><span class="timer">⏳ Ends in ${U.fmtTime(U.secsToNextWeek())}</span><h2>Festival of the Forge</h2><div class="small">The guild smiths celebrate! Forge items to earn milestone rewards.</div></div>
  <div class="panel glow"><div class="row between"><b>Items forged this week</b><b class="gold">${ev.festForged}</b></div>
    ${G.FESTIVAL.map((m, i) => { const done = ev.festClaimed.includes(i), ok = ev.festForged >= m.at; return `<div class="list-row"><div class="lr-ic">${done ? '✅' : '⚒️'}</div><div class="lr-main"><div class="t">Forge ${m.at} items</div>${UI.bar(ev.festForged / m.at * 100)}<div class="mt4">${U.rewardChips(m.reward, 'tiny')}</div></div>
      <button class="btn xs gold ${ok && !done ? '' : 'dis'}" data-act="claimFest" data-arg="${i}">${done ? 'Done' : 'Claim'}</button></div>`; }).join('')}
    <button class="btn ember block mt" data-act="goForge">⚒️ Go Forge</button></div>
  <div class="panel row between"><div><b>📅 Login Calendar</b><div class="tiny mute">Day ${(s.login.count % 7) + (G.canClaimLogin() ? 1 : 0) || 7} of 7</div></div><button class="btn sm gold" data-act="openLogin">Open${UI.dot(G.canClaimLogin())}</button></div>`;
};

/* ---------------- ARTIFACTS ---------------- */
SCREENS.artifacts = () => {
  const s = G.state;
  return `<div class="screen-title"><h1>Artifacts</h1><span class="sub">✨ ${s.res.artFrags} Relic Dust</span></div>
  <div class="panel glow small">Ancient relics grant permanent bonuses to your whole team. Collect fragments from bosses, guild & arena shops and events. <b>2 Relic Dust</b> can be transmuted into 1 fragment of any artifact.</div>
  <div class="grid3">${ARTIFACTS.map(a => {
    const st = s.artifacts[a.id] || { lvl: 0, frags: 0 }; const c = G.artCost(a.id);
    return `<div class="art-card ${st.lvl ? '' : 'locked'}" style="--ac:${a.col}" data-act="artDetail" data-arg="${a.id}"><div class="ai">${a.icon}</div><div class="an">${a.name}</div>
      <div class="tiny ${st.lvl ? 'gold' : 'mute'}">${st.lvl ? 'Lv ' + st.lvl : 'Locked'}</div>${UI.bar(st.lvl >= ARTIFACT_MAX ? 100 : st.frags / c.frags * 100, 'blue')}<div class="tiny mute">${st.lvl >= ARTIFACT_MAX ? 'MAX' : st.frags + '/' + c.frags}</div>
      ${st.lvl < ARTIFACT_MAX && st.frags >= c.frags ? '<i class="dot" style="top:4px;right:6px"></i>' : ''}</div>`;
  }).join('')}</div>`;
};
UI.artDetail = id => {
  const a = ART_BY_ID[id], st = G.state.artifacts[id] || { lvl: 0, frags: 0 }, c = G.artCost(id);
  const pct = a.stat === 'spd' ? '' : '%';
  return `<div class="center"><div class="art-card" style="--ac:${a.col};display:inline-block;width:110px"><div class="ai" style="width:80px;height:80px;font-size:50px">${a.icon}</div></div>
    <h2 class="gold mt4">${a.name}</h2><div class="small">${a.desc}: <b class="ok">+${a.per * st.lvl}${pct}</b>${st.lvl < ARTIFACT_MAX ? ` → <b class="gold">+${a.per * (st.lvl + 1)}${pct}</b>` : ''}</div>
    <div class="small mute">Level ${st.lvl}/${ARTIFACT_MAX} · Fragments ${st.frags}</div></div>
    ${st.lvl < ARTIFACT_MAX ? `<button class="btn gold block mt ${st.frags >= c.frags ? '' : 'dis'}" data-act="artUp" data-arg="${id}">${st.lvl ? 'Upgrade' : 'Unlock'} <span class="cost">🧩${c.frags} · 🪙${U.fmt(c.gold)}</span></button>
    <div class="grid2 mt"><button class="btn sm blue ${G.state.res.artFrags >= 2 ? '' : 'dis'}" data-act="artDust" data-arg="${id}:1">+1 frag <span class="cost">✨2</span></button>
    <button class="btn sm blue ${G.state.res.artFrags >= 20 ? '' : 'dis'}" data-act="artDust" data-arg="${id}:10">+10 frags <span class="cost">✨20</span></button></div>` : '<div class="center gold mt">✦ Max level ✦</div>'}`;
};

/* ---------------- Missions / Achievements / Login / Settings ---------------- */
UI.missions = () => {
  const s = G.state, pts = G.activityPoints();
  return `<div class="panel glow"><div class="row between"><b>Activity</b><b class="gold">${pts}/100</b></div>${UI.bar(pts)}
    <div class="grid5 mt">${ACTIVITY_CHESTS.map((c, i) => { const got = s.daily.chests.includes(i), ok = pts >= c.at; return `<div class="center" data-act="claimChest" data-arg="${i}" style="cursor:pointer;${got ? 'opacity:.4' : ''}"><div style="font-size:26px;${ok && !got ? 'animation:bob 1s infinite' : ''}">${got ? '📭' : '🎁'}</div><div class="tiny ${ok ? 'gold' : 'mute'}">${c.at}</div></div>`; }).join('')}</div>
    <div class="tiny mute center">Resets in ${U.fmtTime(U.secsToNextDay())}</div></div>
    ${DAILY_MISSIONS.map(m => { const p = Math.min(m.goal, s.daily.prog[m.key] || 0), done = s.daily.claimed[m.key];
      return `<div class="list-row"><div class="lr-ic">${done ? '✅' : '📋'}</div><div class="lr-main"><div class="t">${m.name}</div>${UI.bar(p / m.goal * 100, 'green', `${p}/${m.goal}`)}<div class="tiny mute mt4">+${m.pts} activity · 🪙 ${U.fmt(2000 + s.stage * 100)} · 💎10</div></div>
      <button class="btn xs gold ${p >= m.goal && !done ? '' : 'dis'}" data-act="claimMission" data-arg="${m.key}">${done ? 'Done' : 'Claim'}</button></div>`; }).join('')}`;
};
UI.achievements = () => ACHIEVEMENTS.map(a => {
  const t = G.state.ach[a.key] || 0, maxed = t >= a.tiers.length;
  const goal = maxed ? a.tiers[a.tiers.length - 1] : a.tiers[t], v = G.achValue(a.key);
  const r = G.achReward(Math.min(t, a.tiers.length - 1));
  return `<div class="list-row"><div class="lr-ic">🏆</div><div class="lr-main"><div class="t">${a.name} <span class="tiny mute">${t}/${a.tiers.length}</span></div><div class="tiny mute">${a.desc.replace('{n}', U.fmt(goal))}</div>
    ${UI.bar(Math.min(1, v / goal) * 100, '', `${U.fmt(Math.min(v, goal))}/${U.fmt(goal)}`)}${maxed ? '' : `<div class="mt4">${U.rewardChips(r, 'tiny')}</div>`}</div>
    <button class="btn xs gold ${!maxed && v >= goal ? '' : 'dis'}" data-act="claimAch" data-arg="${a.key}">${maxed ? 'Done' : 'Claim'}</button></div>`;
}).join('');
UI.login = () => {
  const s = G.state, cycleDay = s.login.count % 7, can = G.canClaimLogin();
  return `<div class="small mute center mb">Log in every day for escalating rewards. Day 7 is legendary!</div>
    <div class="login-grid">${LOGIN_REWARDS.map((r, i) => {
      const claimed = can ? i < cycleDay : i < (cycleDay === 0 ? 7 : cycleDay);
      const today = can && i === cycleDay;
      return `<div class="lday ${claimed ? 'claimed' : ''} ${today ? 'today' : ''} ${i === 6 ? 'big' : ''}"><div class="b small">Day ${i + 1}</div><div>${U.rewardChips(r)}</div></div>`;
    }).join('')}</div>
    <button class="btn gold block mt ${can ? '' : 'dis'}" data-act="claimLogin">${can ? 'Claim Today\'s Reward' : 'Come back tomorrow · ' + U.fmtTime(U.secsToNextDay())}</button>`;
};
UI.settings = () => {
  const s = G.state;
  const days = Math.floor((Date.now() - s.created) / 86400000);
  return `<div class="panel"><div class="small mute">Adventurer name</div><div class="row gap mt4"><input class="txt-in" id="pname" maxlength="16" value="${U.esc(s.name)}"><button class="btn sm gold" data-act="saveName">Save</button></div></div>
    <div class="panel"><div class="row between"><span>🔊 Sound effects</span><button class="btn sm ${s.settings.sound ? 'green' : 'ghost'}" data-act="toggleSound">${s.settings.sound ? 'ON' : 'OFF'}</button></div>
    <div class="row between mt"><span>⏩ Default battle speed</span><button class="btn sm" data-act="cycleSpeed">×${s.settings.speed}</button></div></div>
    <div class="panel small"><b class="gold">Profile</b><div class="stat-list mt4"><div><span>Account Lv</span><b>${G.playerLevel()}</b></div><div><span>Days played</span><b>${days + 1}</b></div>
      <div><span>Stage</span><b>${Math.min(s.stage, MAX_STAGE)}</b></div><div><span>Heroes</span><b>${G.ownedCount()}</b></div><div><span>Forged</span><b>${s.stats.forged}</b></div><div><span>Arena wins</span><b>${s.stats.arenaWins}</b></div></div></div>
    <div class="panel"><b class="gold">Save Data</b><div class="tiny mute">Progress auto-saves on this device. Copy the code below to back it up or move it to another device.</div>
      <textarea class="txt-in mt4" id="savecode" placeholder="Paste a save code here to import"></textarea>
      <div class="grid2 mt4"><button class="btn sm" data-act="exportSave">📤 Export</button><button class="btn sm" data-act="importSave">📥 Import</button></div>
      <button class="btn sm red block mt" data-act="resetSave">⚠️ Reset all progress</button></div>
    <div class="tiny mute center">Ashen Anvil v1.0 · an original idle RPG</div>`;
};
