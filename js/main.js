/* =========================================================
   App controller: navigation, actions, battles, game loop
   ========================================================= */
'use strict';

const App = {
  screen: 'home', heroFilter: 'all', forgeTab: 'forge', salvageSel: [true, false, false, false, false],
  eqHero: null, eqSlot: 'weapon', invTab: 'mats', invSlot: 'all', chView: null, arenaTab: 'battle',
  guildTab: 'boss', shopTab: 'tavern', guildCreate: false, guildIcon: '⚔️', guildName: '',
  inBattle: false, battle: null, homeScene: null, homeView: null, autoTimer: null,
};

App.go = screen => {
  if (!SCREENS[screen]) return;
  const changed = App.screen !== screen;
  App.screen = screen;
  App.render();
  if (changed) document.getElementById('screen').scrollTop = 0;
};
App.render = () => {
  document.getElementById('topbar').innerHTML = UI.topbar();
  document.getElementById('nav').innerHTML = UI.nav();
  const scr = document.getElementById('screen');
  const st = scr.scrollTop;
  scr.innerHTML = SCREENS[App.screen]();
  scr.scrollTop = st;
  if (App.screen === 'home') App.mountHomeScene(); else App.pauseHome();
  U.refreshModals();
};
App.after = () => { G.save(); App.render(); };

/* ---------------- Idle home scene ---------------- */
App.mountHomeScene = () => {
  const slot = document.getElementById('home-scene-slot');
  if (!slot) return;
  if (!App.homeScene) {
    App.homeScene = document.createElement('div');
    App.homeScene.className = 'home-scene';
    App.homeScene.innerHTML = '<div class="hs-label"></div><div class="hs-gold"></div><div class="hs-arena" style="position:absolute;inset:0"></div>';
  }
  slot.replaceWith(App.homeScene);
  const info = G.stageInfo(Math.min(G.state.stage, MAX_STAGE));
  App.homeScene.querySelector('.hs-label').textContent = `⚔ ${info.chapterName} · Idle Hunt`;
  App.homeScene.querySelector('.hs-gold').textContent = `🪙 ${U.fmt(G.afkRates().gold)}/h`;
  if (App.inBattle) return;
  if (!App.homeView || App.homeView.finished) App.startIdleWave();
  else App.homeView.resume();
};
App.pauseHome = () => { if (App.homeView && !App.homeView.finished) App.homeView.stop(); };
App.startIdleWave = () => {
  if (!App.homeScene) return;
  const left = G.teamUnits();
  if (!left.some(Boolean)) return;
  const n = Math.min(G.state.stage, MAX_STAGE);
  const info = G.stageInfo(n);
  const right = G.stageEnemies(Math.max(1, n - 2)).map(u => u && Object.assign(u, { isBoss: false, hp: u.hp * 0.3, atk: u.atk * 0.22, name: u.name, sprite: ART.monster(u.kind, G.chapterTint(u.kind, info.ch), false) }));
  const b = new Battle(left, right, { timeLimit: 45 });
  App.homeView = new BattleView(App.homeScene.querySelector('.hs-arena'), b, {
    idle: true, theme: info.ch, speed: 1,
    onEnd: bb => {
      if (bb.winner === 0) {
        const g = Math.max(5, Math.round(G.afkRates().gold / 150));
        G.state.res.gold += g;
        const pop = document.createElement('div');
        pop.className = 'gold-pop'; pop.textContent = `+🪙${U.fmt(g)}`;
        pop.style.left = '58%'; pop.style.top = '42%';
        App.homeScene.appendChild(pop);
        setTimeout(() => pop.remove(), 1400);
        const tb = document.querySelector('#topbar .res-pill'); if (tb) tb.innerHTML = `<span class="ic">🪙</span>${U.fmt(G.state.res.gold)}`;
      }
      setTimeout(() => {
        if (App.screen === 'home' && !App.inBattle && document.body.contains(App.homeScene)) App.startIdleWave();
        else App.homeView = null;
      }, 800);
    },
  });
};

/* ---------------- Battle screen ---------------- */
App.startBattle = cfg => {
  if (!cfg.left.some(Boolean)) { U.toast('Your team is empty! Add heroes first.', 'err'); App.go('heroes'); return; }
  U.closeAllModals();
  App.inBattle = true; App.pauseHome();
  const lp = G.unitsPower(cfg.left), rp = G.unitsPower(cfg.right);
  const wrap = document.createElement('div');
  wrap.className = 'battle-screen';
  wrap.innerHTML = `<div class="battle-top"><button class="btn sm ghost" data-act="battleRetreat">✕</button><div class="bt-title"><h3>${U.esc(cfg.title)}</h3><div class="small">${U.esc(cfg.sub || '')}</div></div></div>
    <div class="vs-powers"><span class="ok">⚔ ${U.fmt(lp)}</span><span class="mute">VS</span><span class="bad">☠ ${U.fmt(rp)}</span></div>
    <div class="battle-arena">${cfg.bossMode ? '<div class="dmg-meter">💥 0</div>' : ''}</div>
    <div class="battle-bottom"><span class="timer">0:00</span><div class="grow"></div>
      <button class="btn sm" data-act="battleSpeed">×${G.state.settings.speed}</button><button class="btn sm gold" data-act="battleSkip">Skip ⏭</button></div>`;
  document.getElementById('app').appendChild(wrap);
  const arena = wrap.querySelector('.battle-arena');
  const view_host = document.createElement('div'); view_host.style.cssText = 'position:absolute;inset:0';
  arena.prepend(view_host);
  const b = new Battle(cfg.left, cfg.right, { timeLimit: cfg.timeLimit || 90, bossMode: cfg.bossMode });
  const timerEl = wrap.querySelector('.timer'), meter = wrap.querySelector('.dmg-meter');
  const int = setInterval(() => {
    const left = Math.max(0, b.timeLimit - b.t);
    timerEl.textContent = `${Math.floor(left / 60)}:${String(Math.floor(left % 60)).padStart(2, '0')}`;
    if (meter) meter.textContent = '💥 ' + U.fmt(b.dmgDealt[0]);
  }, 100);
  const view = new BattleView(view_host, b, {
    theme: cfg.theme || 0, speed: G.state.settings.speed,
    onEnd: bb => {
      clearInterval(int);
      if (meter) meter.textContent = '💥 ' + U.fmt(bb.dmgDealt[0]);
      if (App.battle && App.battle.retreated) return;
      bb.winner === 0 ? SFX.play('win') : SFX.play(cfg.bossMode ? 'win' : 'lose');
      cfg.onEnd(bb);
      G.save();
    },
  });
  App.battle = { wrap, view, b, int };
};
App.endBattle = () => {
  if (!App.battle) return;
  clearInterval(App.battle.int);
  App.battle.view.stop();
  App.battle.wrap.remove();
  App.battle = null; App.inBattle = false;
  clearTimeout(App.autoTimer);
  App.render();
};
App.resultModal = (html, opts = {}) => U.modal(html, Object.assign({ cls: 'small', onClose: App.endBattle }, opts));

App.defeatHtml = (title = 'DEFEAT') => `<div class="result"><h1 class="lose">${title}</h1><div class="small mute">Your heroes fell. Grow stronger and try again!</div>
  <div class="grid3 mt"><button class="btn sm" data-act="tipGo" data-arg="heroes">🛡️ Level Heroes</button><button class="btn sm" data-act="tipGo" data-arg="forge">⚒️ Forge Gear</button><button class="btn sm" data-act="tipGo" data-arg="equipment">🗡️ Equip</button></div>
  <button class="btn ghost block mt" data-act="closeModal">Continue</button></div>`;

/* =========================================================
   ACTIONS — every data-act maps here
   ========================================================= */
const ACT = {};
const err = m => { U.toast(m, 'err'); SFX.play('error'); };

ACT.noop = () => { };
ACT.go = k => { U.closeAllModals(); App.go(k); };
ACT.tipGo = k => { U.closeAllModals(); App.go(k); };
ACT.closeModal = () => U.closeModal();
ACT.confirmYes = () => { const cb = U._confirmCb; U._confirmCb = null; U.closeModal(); if (cb) cb(); };

/* ---- Home ---- */
ACT.collectAfk = () => {
  if (G.afkSeconds() < 60) return err('Your heroes need at least a minute to gather loot.');
  const { rew, drops, secs } = G.collectAfk();
  SFX.play('reward');
  U.modal(`<div class="result rays"><div style="width:120px;margin:0 auto">${ART.chest(true)}</div><div class="small mute">Gathered over ${U.fmtTime(secs)}</div>
    <div class="mt">${U.rewardChips(rew)}</div>${drops.length ? `<div class="grid5 mt">${drops.map(d => UI.gearSlot(d)).join('')}</div>` : ''}
    <button class="btn gold block mt" data-act="closeModal">Excellent!</button></div>`, { title: 'Idle Loot', cls: 'small' });
  App.after();
};
ACT.quickLoot = () => {
  const c = G.quickLootCost();
  const run = () => { const r = G.quickLoot(); if (!r) return err('Not enough gems.'); SFX.play('reward'); U.toast('⚡ ' + U.rewardChips(r)); App.after(); };
  if (c) U.confirm('Spend 💎50 for 2 hours of idle loot instantly?', run); else run();
};

/* ---- Campaign ---- */
ACT.stagePreview = n => { n = +n; U.modal(UI.stagePreview(n), { title: 'Stage ' + n, cls: 'small' }); };
ACT.stageInfo = n => U.toast(`Stage ${n} already conquered ✓`);
ACT.chNav = d => { App.chView += +d; App.render(); };
ACT.toggleAutoPush = () => { G.state.settings.autoPush = !G.state.settings.autoPush; App.after(); };
ACT.fightStage = n => {
  n = +n;
  if (n !== G.state.stage) return err('That stage is not available.');
  const info = G.stageInfo(n);
  App.startBattle({
    left: G.teamUnits(), right: G.stageEnemies(n), theme: info.ch,
    title: info.isBoss ? '💀 ' + info.bossName : `Stage ${info.ch + 1}-${info.idx}`, sub: info.chapterName,
    onEnd: b => {
      if (b.winner === 0) {
        const { rew, drops } = G.clearStage(n);
        const more = G.state.stage <= MAX_STAGE;
        App.resultModal(`<div class="result rays"><h1 class="win">VICTORY</h1><div class="small mute">${info.isBoss ? info.bossName + ' has fallen!' : 'Stage ' + info.idx + ' cleared'}</div>
          <div class="mt">${U.rewardChips(rew)}</div>${drops.length ? `<div class="grid5 mt" style="max-width:${drops.length * 64}px;margin:10px auto 0;grid-template-columns:repeat(${drops.length},1fr)">${drops.map(d => UI.gearSlot(d)).join('')}</div>` : ''}
          <div class="row gap mt"><button class="btn ghost grow" data-act="closeModal">Continue</button>${more ? `<button class="btn ember grow" data-act="nextStage">Next ▶${G.state.settings.autoPush ? ' <span class="cost" id="autoCount">(auto)</span>' : ''}</button>` : ''}</div></div>`);
        if (G.state.settings.autoPush && more) App.autoTimer = setTimeout(() => { if (App.battle) ACT.nextStage(); }, 2200);
      } else {
        App.resultModal(App.defeatHtml());
      }
    },
  });
};
ACT.nextStage = () => { clearTimeout(App.autoTimer); U.closeAllModals(); setTimeout(() => ACT.fightStage(G.state.stage), 50); };

/* ---- Battle controls ---- */
ACT.battleSpeed = (a, el) => {
  const sp = { 1: 2, 2: 3, 3: 1 }[G.state.settings.speed] || 1;
  G.state.settings.speed = sp;
  if (App.battle) App.battle.view.setSpeed(sp);
  el.textContent = '×' + sp;
};
ACT.battleSkip = () => { if (App.battle && !App.battle.view.finished) App.battle.view.skip(); };
ACT.battleRetreat = () => {
  if (!App.battle) return;
  if (App.battle.view.finished) { U.closeAllModals(); return; }
  U.confirm('Retreat from this battle? It will count as a loss.', () => {
    App.battle.retreated = true;
    const b = App.battle.b, cfgEnd = App.battle;
    App.battle.view.stop();
    App.endBattle();
    void b; void cfgEnd;
  }, 'Retreat');
};

/* ---- Missions etc ---- */
ACT.openMissions = () => U.modal(UI.missions(), { title: '📋 Daily Missions', render: UI.missions });
ACT.openAch = () => U.modal(UI.achievements(), { title: '🏆 Achievements', render: UI.achievements });
ACT.openLogin = () => U.modal(UI.login(), { title: '📅 Login Rewards', render: UI.login });
ACT.openSettings = () => U.modal(UI.settings(), { title: '⚙️ Settings' });
ACT.claimMission = k => { if (G.claimMission(k)) { SFX.play('reward'); U.toast('Mission complete! +10 💎'); App.after(); } };
ACT.claimChest = i => { const r = G.claimChest(+i); if (!r) return err('Not enough activity points yet.'); SFX.play('reward'); U.toast('🎁 ' + U.rewardChips(r)); App.after(); };
ACT.claimAch = k => { const r = G.claimAch(k); if (r) { SFX.play('reward'); U.toast('🏆 ' + U.rewardChips(r)); App.after(); } };
ACT.claimLogin = () => { const r = G.claimLogin(); if (!r) return; SFX.play('legend'); U.toast('📅 ' + U.rewardChips(r)); App.after(); };

/* ---- Settings ---- */
ACT.saveName = () => { const v = (document.getElementById('pname').value || '').trim().slice(0, 16); if (v.length < 2) return err('Name too short.'); G.state.name = v; U.toast('Name saved'); App.after(); };
ACT.toggleSound = () => { G.state.settings.sound = SFX.enabled = !G.state.settings.sound; App.after(); U.closeModal(); ACT.openSettings(); };
ACT.cycleSpeed = (a, el) => { G.state.settings.speed = { 1: 2, 2: 3, 3: 1 }[G.state.settings.speed] || 1; el.textContent = '×' + G.state.settings.speed; G.save(); };
ACT.exportSave = () => {
  const code = G.exportSave(); const ta = document.getElementById('savecode'); ta.value = code; ta.select();
  const fallback = () => U.toast('Save code ready — copy it from the box');
  try { navigator.clipboard.writeText(code).then(() => U.toast('Save code copied to clipboard'), fallback); } catch (e) { fallback(); }
};
ACT.importSave = () => {
  const v = document.getElementById('savecode').value;
  if (!v.trim()) return err('Paste a save code first.');
  try { G.importSave(v); U.closeAllModals(); App.homeView = null; U.toast('Save imported!', 'ok'); App.render(); } catch (e) { err('Invalid save code.'); }
};
ACT.resetSave = () => U.confirm('Erase ALL progress and start over? This cannot be undone.', () => { G.reset(); U.closeAllModals(); if (App.homeView) App.homeView.stop(); App.homeView = null; App.go('home'); U.toast('A new legend begins…'); }, 'Erase');

/* ---- Heroes ---- */
ACT.heroFilter = k => { App.heroFilter = k; App.render(); };
ACT.heroDetail = id => U.modal(UI.heroDetail(id), { title: HERO_BY_ID[id].name, render: () => UI.heroDetail(id) });
ACT.pickTeamSlot = i => {
  i = +i;
  const owned = Object.keys(G.state.heroes).sort((a, b) => G.heroPower(G.state.heroes[b]) - G.heroPower(G.state.heroes[a]));
  const cur = G.state.team[i];
  U.modal(`<div class="small mute mb">Choose a hero for slot ${i + 1} (${i < 2 ? 'Front row — targeted first' : 'Back row'}).</div>
    <div class="grid4">${owned.map(id => { const at = G.state.team.indexOf(id); return UI.heroCard(id, { act: 'setTeam', arg: `${i}:${id}`, noTeam: true, sel: id === cur, badge: at >= 0 ? 'SLOT ' + (at + 1) : '', badgeCol: 'var(--gold)' }); }).join('')}</div>
    <div class="row gap mt">${cur ? `<button class="btn red grow" data-act="setTeam" data-arg="${i}:">Remove</button>` : ''}${cur ? `<button class="btn grow" data-act="heroDetail" data-arg="${cur}">View hero</button>` : ''}</div>`, { title: 'Formation' });
};
ACT.setTeam = arg => {
  const [i, id] = arg.split(':');
  if (!id && G.state.team.filter(Boolean).length <= 1) return err('Your team needs at least one hero.');
  G.setTeamSlot(+i, id || null);
  U.closeModal(); App.homeView && App.homeView.stop(); App.homeView = null; App.after();
};
ACT.toggleTeam = id => {
  const t = G.state.team, at = t.indexOf(id);
  if (at >= 0) { if (t.filter(Boolean).length <= 1) return err('Your team needs at least one hero.'); t[at] = null; }
  else { const free = t.indexOf(null); if (free < 0) return err('Team is full. Tap a formation slot to swap.'); t[free] = id; }
  App.homeView && App.homeView.stop(); App.homeView = null; App.after();
};
ACT.autoTeam = () => {
  const owned = Object.keys(G.state.heroes).sort((a, b) => G.heroPower(G.state.heroes[b]) - G.heroPower(G.state.heroes[a])).slice(0, 5);
  const pr = { warrior: 0, support: 1, archer: 2, mage: 3, assassin: 4 };
  owned.sort((a, b) => pr[HERO_BY_ID[a].cls] - pr[HERO_BY_ID[b].cls]);
  G.state.team = [0, 1, 2, 3, 4].map(i => owned[i] || null);
  App.homeView && App.homeView.stop(); App.homeView = null;
  U.toast('Strongest formation assembled!'); App.after();
};
ACT.unlockHero = id => { const e = G.unlockHero(id); if (e) return err(e); SFX.play('legend'); U.toast(`${HERO_BY_ID[id].name} joins your cause!`, 'ok'); App.after(); };
ACT.levelUp = arg => {
  const [id, n] = arg.split(':');
  const done = G.levelUp(id, +n);
  if (!done) { const hi = G.state.heroes[id]; return err(hi.lvl >= G.maxLevel(hi) ? 'Max level — evolve to raise the cap.' : 'Missing: ' + G.missing(G.levelCost(hi.lvl))); }
  SFX.play('reward'); U.toast(`Level +${done}!`); App.after();
};
ACT.evolve = id => { const e = G.evolve(id); if (e) return err(e); SFX.play('legend'); U.toast('⭐ Evolved!', 'ok'); App.after(); };
ACT.skillUp = id => { const e = G.skillUp(id); if (e) return err(e); SFX.play('reward'); U.toast('Skill upgraded!'); App.after(); };
ACT.awaken = id => { const e = G.awaken(id); if (e) return err(e); SFX.play('legend'); U.toast('🔮 Awakened!', 'ok'); App.after(); };
ACT.autoEquip = id => { const n = G.autoEquip(id); U.toast(n ? `Equipped ${n} better item${n > 1 ? 's' : ''}` : 'Already wearing the best gear'); App.after(); };
ACT.autoEquipTeam = () => { let n = 0; G.state.team.filter(Boolean).forEach(id => { n += G.autoEquip(id); }); U.toast(n ? `Equipped ${n} upgrades across the team` : 'Team already has the best gear'); App.after(); };
ACT.unequipAll = id => { G.gearOf(G.state.heroes[id]).forEach(g => G.unequip(g.uid)); App.after(); };
ACT.pickGear = arg => { const [id, sl] = arg.split(':'); U.modal(UI.gearPicker(id, sl), { title: SLOT_INFO[sl].name, render: () => UI.gearPicker(id, sl) }); };
ACT.equipItem = arg => { const [id, uid] = arg.split(':'); G.equip(id, +uid); SFX.play('forge'); const top = document.querySelector('#modal-root .modal-wrap:last-child h2'); if (top && (SLOTS.some(s => SLOT_INFO[s].name === top.textContent) || top.textContent === 'Equip on…')) U.closeModal(); App.after(); };
ACT.unequip = uid => { G.unequip(+uid); App.after(); };

/* ---- Items ---- */
ACT.itemDetail = uid => { if (!G.gearById(+uid)) return; U.modal(UI.itemDetail(+uid), { title: 'Equipment', render: () => UI.itemDetail(+uid) }); };
ACT.enhance = arg => {
  const [uid, n] = arg.split(':').map(Number); let done = 0, bonus = null, e = null;
  for (let i = 0; i < n; i++) { const r = G.enhance(uid); if (typeof r === 'string') { e = r; break; } done++; if (r.bonus) bonus = r.bonus; }
  if (!done) return err(e);
  SFX.play('forge');
  U.toast(`Enhanced +${done}` + (bonus ? ` · ${STAT_NAMES[bonus.stat]} boosted!` : ''));
  App.after();
};
ACT.toggleLock = uid => { const it = G.gearById(+uid); it.locked = !it.locked; App.after(); };
ACT.salvageOne = uid => { const r = G.salvage([+uid]); U.closeModal(); U.toast('♻️ ' + U.rewardChips(r)); App.after(); };
ACT.equipTo = uid => {
  const it = G.gearById(+uid);
  const ids = Object.keys(G.state.heroes).sort((a, b) => G.state.team.includes(b) - G.state.team.includes(a));
  U.modal(`<div class="small mute mb">Choose a hero to wear this ${SLOT_INFO[it.slot].name.toLowerCase()}.</div>${ids.map(id => {
    const hi = G.state.heroes[id], others = G.gearOf(hi).filter(g => g.slot !== it.slot);
    const d = G.statPower(G.heroStats(hi, others.concat([it]))) - G.heroPower(hi);
    return `<div class="list-row"><div style="width:44px;flex:none">${UI.heroCard(id, { act: 'noop' })}</div><div class="lr-main"><div class="t rtext${HERO_BY_ID[id].rarity}">${HERO_BY_ID[id].name}</div><div class="small ${d >= 0 ? 'cmp-up' : 'cmp-down'}">${d >= 0 ? '▲' : '▼'} ${U.fmt(Math.abs(d))} power</div></div><button class="btn sm gold" data-act="equipItem" data-arg="${id}:${uid}">Equip</button></div>`;
  }).join('')}`, { title: 'Equip on…' });
};

/* ---- Forge ---- */
ACT.forgeTab = k => { App.forgeTab = k; App.render(); };
ACT.goForge = () => { U.closeAllModals(); App.forgeTab = 'forge'; App.go('forge'); };
ACT.upgradeForge = () => {
  if (G.state.forge.lvl >= G.forgeMax) return;
  if (!G.spend(G.forgeUpgradeCost())) return err('Not enough gold.');
  G.state.forge.lvl++; SFX.play('legend'); U.toast(`🔥 Forge upgraded to level ${G.state.forge.lvl}!`, 'ok'); App.after();
};
ACT.forge = n => {
  n = +n;
  if (G.state.res.ore < 1) return err('Out of Ember Ore! Earn more from battles and idle loot.');
  const items = G.forge(n);
  const hero = document.getElementById('forge-hero');
  if (hero) {
    hero.classList.remove('striking'); void hero.offsetWidth; hero.classList.add('striking');
    const sp = document.getElementById('sparks'); sp.innerHTML = '';
    for (let i = 0; i < 18; i++) { const s = document.createElement('i'); const a = Math.random() * Math.PI * 2, d = 30 + Math.random() * 70; s.style.setProperty('--dx', Math.cos(a) * d + 'px'); s.style.setProperty('--dy', Math.sin(a) * d - 20 + 'px'); sp.appendChild(s); }
  }
  SFX.play('forge');
  const best = Math.max(...items.map(i => i.rarity));
  if (best >= 3) setTimeout(() => SFX.play('legend'), 250);
  setTimeout(() => {
    G.save();
    if (items.length === 1) {
      const it = items[0];
      U.modal(`<div class="result ${it.rarity >= 3 ? 'rays' : ''}"><div class="small rtext${it.rarity} b">${RARITIES[it.rarity].name.toUpperCase()} FORGED!</div><div class="mt">${UI.itemSummary(it)}</div>
        <div class="grid3 mt"><button class="btn sm green" data-act="equipTo" data-arg="${it.uid}">Equip…</button><button class="btn sm red" data-act="salvageOne" data-arg="${it.uid}">Salvage</button><button class="btn sm gold" data-act="closeModal">Keep</button></div></div>`, { title: '⚒️ Forged', cls: 'small' });
    } else {
      const counts = [0, 0, 0, 0, 0]; items.forEach(i => counts[i.rarity]++);
      U.modal(`<div class="center small mb">${counts.map((c, i) => c ? `<span class="chip rtext${i}">${RARITIES[i].name} ×${c}</span>` : '').join('')}</div>
        <div class="reveal-grid">${items.sort((a, b) => b.rarity - a.rarity).map((it, i) => UI.gearSlot(it).replace('class="gslot', `style="animation-delay:${Math.min(i, 20) * 0.04}s" class="gslot`)).join('')}</div>
        <div class="grid2 mt"><button class="btn sm gold" data-act="autoEquipTeamClose">✨ Auto-equip Team</button><button class="btn sm red" data-act="salvageCommonBatch" data-arg="${items.map(i => i.uid).join(',')}">♻️ Salvage Commons</button></div>
        <button class="btn ghost block mt" data-act="closeModal">Done</button>`, { title: `⚒️ Forged ×${items.length}` });
    }
    App.render();
  }, 330);
};
ACT.autoEquipTeamClose = () => { U.closeModal(); ACT.autoEquipTeam(); };
ACT.salvageCommonBatch = list => {
  const ids = list.split(',').map(Number).filter(uid => { const g = G.gearById(uid); return g && g.rarity === 0 && !g.owner && !g.locked; });
  if (!ids.length) return err('No unequipped commons in this batch.');
  const r = G.salvage(ids); U.closeModal(); U.toast(`♻️ ${ids.length} items → ` + U.rewardChips(r)); App.after();
};
ACT.combine = arg => {
  const [sl, r] = arg.split(':');
  const it = G.combine(sl, +r);
  if (!it) return err(+r >= 4 ? 'Mythic is the highest rarity.' : 'Need 3 free items of this slot and rarity.');
  SFX.play(it.rarity >= 3 ? 'legend' : 'forge');
  G.save();
  U.modal(`<div class="result ${it.rarity >= 3 ? 'rays' : ''}"><div class="small rtext${it.rarity} b">COMBINED INTO ${RARITIES[it.rarity].name.toUpperCase()}!</div><div class="mt">${UI.itemSummary(it)}</div><button class="btn gold block mt" data-act="closeModal">Great!</button></div>`, { title: '🔀 Combine', cls: 'small' });
  App.render();
};
ACT.combineAll = () => {
  const made = [];
  for (let r = 0; r < 4; r++) for (const sl of SLOTS) { let it; while ((it = G.combine(sl, r))) made.push(it); }
  if (!made.length) return err('Nothing to combine.');
  SFX.play('legend'); G.save();
  U.modal(`<div class="center small mb">Created ${made.length} upgraded items</div><div class="reveal-grid">${made.map(it => UI.gearSlot(it)).join('')}</div><button class="btn gold block mt" data-act="closeModal">Great!</button>`, { title: '🔀 Combine All' });
  App.render();
};
ACT.salvSel = i => { App.salvageSel[+i] = !App.salvageSel[+i]; App.render(); };
ACT.salvageBulk = () => {
  const ids = G.state.gear.filter(g => !g.owner && !g.locked && App.salvageSel[g.rarity]).map(g => g.uid);
  if (!ids.length) return;
  const hasHigh = App.salvageSel[3] || App.salvageSel[4];
  const run = () => { const r = G.salvage(ids); SFX.play('forge'); U.toast(`♻️ ${ids.length} items → ` + U.rewardChips(r)); App.after(); };
  if (hasHigh) U.confirm('You selected Legendary/Mythic items. Salvage them anyway?', run, 'Salvage'); else run();
};

/* ---- Equipment / Inventory ---- */
ACT.eqHero = id => { App.eqHero = id; App.render(); };
ACT.eqSlot = sl => { App.eqSlot = sl; App.render(); };
ACT.invTab = k => { App.invTab = k; App.render(); };
ACT.invSlot = k => { App.invSlot = k; App.render(); };
ACT.resInfo = k => U.modal(`<div class="center"><div style="font-size:54px">${RES[k].icon}</div><h2 class="gold">${RES[k].name}</h2><div class="b" style="font-size:20px">${U.fmt(G.state.res[k])}</div><p class="small mute">${RES[k].desc}</p></div><button class="btn block" data-act="closeModal">Close</button>`, { cls: 'small' });
ACT.useItem = arg => {
  const [k, n] = arg.split(':');
  const times = n === 'all' ? G.state.items[k] : 1;
  const total = {};
  for (let i = 0; i < times; i++) { const r = G.useItem(k); if (!r) break; for (const x in r) total[x] = (total[x] || 0) + r[x]; }
  if (!Object.keys(total).length) return err('None left.');
  SFX.play('reward');
  U.modal(`<div class="result rays"><div style="font-size:50px">${CONSUMABLES[k].icon}</div><div class="mt">${U.rewardChips(total)}</div><button class="btn gold block mt" data-act="closeModal">Nice!</button></div>`, { title: CONSUMABLES[k].name, cls: 'small' });
  App.after();
};

/* ---- Arena ---- */
ACT.arenaTab = k => { App.arenaTab = k; App.render(); };
ACT.buyTicket = () => { if (!G.spend({ gems: 50 })) return err('Not enough gems.'); G.state.res.tickets++; App.after(); };
ACT.refreshOpps = () => { G.genOpponents(); App.after(); };
ACT.arenaFight = i => {
  const opp = G.state.arena.opps[+i];
  if (!opp || opp.done) return;
  if (G.state.res.tickets < 1) return err('No Arena tickets left. Buy more or wait for the daily refill.');
  if (!G.state.team.some(Boolean)) return err('Your team is empty.');
  G.state.res.tickets--;
  App.startBattle({
    left: G.teamUnits(), right: G.virtualTeam(opp.seed, opp.power), theme: 3,
    title: '🏟️ Arena vs ' + opp.name, sub: `Rating ${opp.rating}`,
    onEnd: b => {
      const win = b.winner === 0;
      const r = G.arenaResult(opp, win);
      App.resultModal(`<div class="result ${win ? 'rays' : ''}"><h1 class="${win ? 'win' : 'lose'}">${win ? 'VICTORY' : 'DEFEAT'}</h1>
        <div class="b" style="font-size:20px">${G.state.arena.rating} <span class="${r.delta >= 0 ? 'ok' : 'bad'}">(${r.delta >= 0 ? '+' : ''}${r.delta})</span></div>
        <div class="mt">${U.rewardChips({ arenaCoins: r.coins })}</div><button class="btn gold block mt" data-act="closeModal">Continue</button></div>`);
    },
  });
};
ACT.claimArenaWeekly = () => {
  const p = G.state.arena.pendingReward; if (!p) return;
  G.grant({ gems: p.gems, arenaCoins: p.arenaCoins, scrolls: p.scrolls }); G.state.arena.pendingReward = null;
  SFX.play('legend'); U.toast('🏅 Weekly rewards claimed!', 'ok'); App.after();
};

/* ---- Guild ---- */
ACT.guildTab = k => { App.guildTab = k; App.render(); };
ACT.guildCreateToggle = () => { App.guildCreate = !App.guildCreate; App.render(); };
ACT.guildIcon = ic => { App.guildIcon = ic; App.render(); };
ACT.createGuild = () => { const e = G.createGuild(App.guildName, App.guildIcon); if (e) return err(e); App.guildCreate = false; SFX.play('legend'); U.toast('Your guild is founded!', 'ok'); App.after(); };
ACT.joinGuild = id => { const cat = G.guildCatalog()[+id]; const e = G.joinGuild(cat); if (e) return err(e); SFX.play('legend'); U.toast(`Welcome to ${cat.name}!`, 'ok'); App.guildTab = 'boss'; App.after(); };
ACT.leaveGuild = () => U.confirm('Leave your guild? Your contribution will be lost.', () => { G.leaveGuild(); App.after(); }, 'Leave');
ACT.donate = i => { const e = G.donate(+i); if (e) return err(e); SFX.play('reward'); U.toast('Thank you for your generosity!'); App.after(); };
ACT.guildBossFight = () => {
  const gd = G.state.guild; if (!gd || gd.attempts < 1) return err('No attempts left today.');
  gd.attempts--;
  App.startBattle({
    left: G.teamUnits(), right: [G.guildBossUnit()], theme: 4, timeLimit: 30, bossMode: true,
    title: '🐉 ' + G.guildBossName(), sub: 'Guild Boss · Lv ' + gd.bossLvl,
    onEnd: b => {
      const dmg = Math.round(b.dmgDealt[0]);
      const coins = Math.min(400, 30 + Math.round(dmg / gd.bossMax * 1500));
      G.grant({ guildCoins: coins, gold: Math.round(3000 * G.goldMult()) });
      const kill = G.guildBossDamage(dmg);
      G.track('guild', 1);
      App.resultModal(`<div class="result rays"><h1 class="win">${kill ? 'BOSS SLAIN!' : 'TIME!'}</h1><div class="small mute">Damage dealt</div><div class="b gold" style="font-size:26px">💥 ${U.fmt(dmg)}</div>
        <div class="mt">${U.rewardChips(Object.assign({ guildCoins: coins, gold: Math.round(3000 * G.goldMult()) }, kill || {}))}</div><button class="btn gold block mt" data-act="closeModal">Continue</button></div>`);
    },
  });
};
ACT.warFight = i => {
  const gd = G.state.guild; const w = G.ensureWar(); const d = w.defenders[+i];
  if (gd.warAttacks < 1) return err('No war attacks left today.');
  gd.warAttacks--;
  App.startBattle({
    left: G.teamUnits(), right: G.virtualTeam(d.seed, d.power), theme: 5,
    title: '⚔️ War vs ' + w.enemy, sub: 'Defender: ' + d.name,
    onEnd: b => {
      const alive = b.winner === 0 ? b.alive(0).length : 0;
      const r = G.warResult(+i, alive);
      App.resultModal(`<div class="result ${alive ? 'rays' : ''}"><h1 class="${alive ? 'win' : 'lose'}">${alive ? 'VICTORY' : 'DEFEAT'}</h1><div class="stars" style="font-size:30px">${'★'.repeat(r.stars)}<span class="mute">${'☆'.repeat(3 - r.stars)}</span></div>
        <div class="small">+${r.gained} war stars</div><button class="btn gold block mt" data-act="closeModal">Continue</button></div>`);
    },
  });
};
ACT.claimWar = () => {
  const p = G.state.guild.pendingWar; if (!p) return;
  G.grant(p.win ? { guildCoins: 600, gems: 300 } : { guildCoins: 200, gems: 80 }); G.state.guild.pendingWar = null;
  SFX.play('legend'); App.after();
};

/* ---- Shop ---- */
ACT.shopTab = k => { App.shopTab = k; App.render(); };
ACT.goShopTab = k => { U.closeAllModals(); App.shopTab = k; App.go('shop'); };
ACT.summon = n => {
  n = +n;
  const useScrolls = G.state.res.scrolls >= n;
  const res = G.summon(n, useScrolls ? 'scrolls' : 'gems');
  if (!res) return err(`Need ${n} scroll${n > 1 ? 's' : ''} or ${n === 10 ? '1,350' : '150'} gems.`);
  const best = Math.max(...res.map(r => r.rarity));
  SFX.play(best >= 3 ? 'legend' : 'reward');
  G.save();
  U.modal(`<div class="result ${best >= 3 ? 'rays' : ''}"><div class="summon-res" style="${n === 1 ? 'grid-template-columns:1fr;max-width:130px;margin:0 auto' : ''}">${res.map((r, i) =>
    UI.heroCard(r.id, { act: 'heroDetail', noTeam: true, badge: r.dupe ? `+${r.shards}🧩` : 'NEW!', badgeCol: r.dupe ? 'var(--blue)' : 'var(--ok)' }).replace('class="hcard', `style="animation-delay:${i * 0.08}s" class="hcard`)).join('')}</div>
    <div class="row gap mt"><button class="btn ghost grow" data-act="closeModal">Done</button><button class="btn gold grow" data-act="summonAgain" data-arg="${n}">Again ×${n}</button></div></div>`, { title: '📜 Summon Results' });
  App.render();
};
ACT.summonAgain = n => { U.closeModal(); ACT.summon(n); };
ACT.freeGems = () => { if (G.state.shop.freeGems === G.state.day) return; G.state.shop.freeGems = G.state.day; G.grant({ gems: 50 }); SFX.play('reward'); U.toast('+50 💎'); App.after(); };
ACT.freeGold = () => { if (G.state.shop.freeGold === G.state.day) return; G.state.shop.freeGold = G.state.day; const g = G.afkRates().gold; G.grant({ gold: g }); SFX.play('reward'); U.toast('+🪙 ' + U.fmt(g)); App.after(); };
ACT.buyDeal = i => {
  const d = G.state.shop.deals[+i]; if (!d || G.state.shop.bought[i]) return;
  if (!G.buy(G.dealCost(d), d.give)) return err('Missing: ' + G.missing(G.dealCost(d)));
  G.state.shop.bought[i] = true; SFX.play('reward'); U.toast('Purchased! ' + U.rewardChips(d.give)); App.after();
};
ACT.exchange = k => {
  const map = { ore: [{ gems: 40 }, { ore: 10 }], steel: [{ gems: 40 }, { steel: 50 }], essence: [{ gems: 60 }, { essence: G.afkRates().essence * 4 }], tomes: [{ gems: 30 }, { tomes: 1 }] };
  const [c, g] = map[k];
  if (!G.buy(c, g)) return err('Not enough gems.');
  SFX.play('reward'); U.toast(U.rewardChips(g)); App.after();
};
ACT.buyLimited = arg => {
  const [store, id] = arg.split(':');
  const list = store === 'arena' ? G.ARENA_SHOP : G.GUILD_SHOP;
  const bag = store === 'arena' ? G.state.shop.arenaBought : G.state.shop.guildBought;
  const it = list.find(x => x.id === id);
  const e = G.buyLimited(it, bag); if (e) return err(e);
  SFX.play('reward'); U.toast('Purchased! ' + U.rewardChips(it.give)); App.after();
};

/* ---- Events ---- */
ACT.siegeFight = () => {
  const tier = G.state.events.siege; if (tier > G.SIEGE_MAX) return;
  App.startBattle({
    left: G.teamUnits(), right: G.siegeEnemies(tier), theme: 4,
    title: `🌑 Blood Moon Siege · Tier ${tier}`, sub: 'Limited-time event',
    onEnd: b => {
      if (b.winner === 0) {
        const r = G.siegeReward(tier); G.grant(r); G.state.events.siege++;
        App.resultModal(`<div class="result rays"><h1 class="win">VICTORY</h1><div class="small mute">Tier ${tier} repelled!</div><div class="mt">${U.rewardChips(r)}</div><button class="btn gold block mt" data-act="closeModal">Continue</button></div>`);
      } else App.resultModal(App.defeatHtml());
    },
  });
};
ACT.buyEvent = id => { const it = G.EVENT_SHOP.find(x => x.id === id); const e = G.buyLimited(it, G.state.events.shopBought); if (e) return err(e); SFX.play('reward'); U.toast('Purchased! ' + U.rewardChips(it.give)); App.after(); };
ACT.claimFest = i => {
  const ev = G.state.events, m = G.FESTIVAL[+i];
  if (ev.festClaimed.includes(+i) || ev.festForged < m.at) return;
  ev.festClaimed.push(+i); G.grant(m.reward); SFX.play('reward'); U.toast('🎉 ' + U.rewardChips(m.reward)); App.after();
};

/* ---- Artifacts ---- */
ACT.artDetail = id => U.modal(UI.artDetail(id), { title: 'Artifact', cls: 'small', render: () => UI.artDetail(id) });
ACT.artUp = id => { const e = G.upgradeArtifact(id); if (e) return err(e); SFX.play('legend'); U.toast(`${ART_BY_ID[id].name} empowered!`, 'ok'); App.after(); };
ACT.artDust = arg => { const [id, n] = arg.split(':'); const e = G.dustToFrag(id, +n); if (e) return err(e); SFX.play('reward'); App.after(); };

/* =========================================================
   Boot
   ========================================================= */
App.tick = () => {
  if (!G.state) return;
  const msgs = G.checkResets();
  if (msgs.length) { if (msgs.includes('weekly')) U.toast('🗓️ A new week begins! Weekly rewards await.', 'ok'); else U.toast('☀️ A new day — missions and tickets refreshed!', 'ok'); App.render(); }
  if (App.screen === 'home' && !App.inBattle) {
    const t = document.getElementById('afk-time'), p = document.getElementById('afk-pend');
    if (t) { const secs = G.afkSeconds(); t.textContent = `${U.fmtTime(secs)} / ${AFK_CAP_H}h`; const pend = G.afkPending(secs); p.innerHTML = U.rewardChips({ gold: pend.gold, essence: pend.essence, ore: pend.ore, steel: pend.steel, gems: pend.gems }); }
  }
};

App.init = () => {
  const existed = G.load();
  SFX.enabled = G.state.settings.sound;
  const away = (Date.now() - G.state.lastSeen) / 1000;
  G.checkResets();
  document.addEventListener('click', e => {
    const el = e.target.closest('[data-act]');
    if (!el) return;
    const act = el.dataset.act, fn = ACT[act];
    if (!fn) return;
    if (act !== 'noop') SFX.play('click');
    try { fn(el.dataset.arg, el); } catch (ex) { console.error(ex); err('Something went wrong: ' + ex.message); }
  });
  document.addEventListener('input', e => { if (e.target.id === 'gname') App.guildName = e.target.value; });
  document.addEventListener('visibilitychange', () => { if (document.hidden) G.save(); else App.tick(); });
  window.addEventListener('pagehide', G.save);
  window.addEventListener('beforeunload', G.save);
  // Android back button / browser back: close modal first
  history.pushState({ g: 1 }, '');
  window.addEventListener('popstate', () => {
    history.pushState({ g: 1 }, '');
    if (document.querySelector('#modal-root .modal-wrap')) U.closeModal();
    else if (App.battle) ACT.battleRetreat();
    else if (App.screen !== 'home') App.go('home');
  });
  App.render();
  setInterval(App.tick, 1000);
  setInterval(G.save, 15000);
  G.save();
  const loader = document.getElementById('loading');
  setTimeout(() => { loader.classList.add('out'); setTimeout(() => loader.remove(), 600); }, 700);
  if (!existed) {
    setTimeout(() => U.modal(`<div class="center"><div style="width:120px;margin:0 auto">${ART.hero('torvald')}</div>
      <p>The Ashen King's fire has consumed the realm. Monsters pour from the cinders, and only the forge-born heroes stand against them.</p>
      <p class="small mute">Your heroes fight on their own — even while you sleep. Forge mighty gear, gather legends and reclaim the Ashen Crown!</p>
      <div class="mt">${U.rewardChips({ gems: 600, scrolls: 5, ore: 40 })}</div>
      <button class="btn ember lg block mt" data-act="closeModal">Begin the Journey</button></div>`, { title: 'Welcome, Wanderer', cls: 'small' }), 900);
  } else if (away > 300) {
    setTimeout(() => {
      const secs = G.afkSeconds(), pend = G.afkPending(secs);
      U.modal(`<div class="center"><div style="width:110px;margin:0 auto">${ART.chest(true)}</div><p>While you were away for <b class="gold">${U.fmtTime(away)}</b>, your heroes kept fighting!</p>
        <div>${U.rewardChips(pend)}</div><button class="btn gold block mt" data-act="welcomeCollect">Collect</button></div>`, { title: 'Welcome Back!', cls: 'small' });
    }, 900);
  }
};
ACT.welcomeCollect = () => { U.closeModal(); ACT.collectAfk(); };

if ('serviceWorker' in navigator && location.protocol.startsWith('http') && !window.Capacitor) {
  window.addEventListener('load', () => navigator.serviceWorker.register('sw.js').catch(() => { }));
}
window.addEventListener('DOMContentLoaded', App.init);
