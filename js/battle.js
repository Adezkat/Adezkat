/* =========================================================
   Battle engine (pure simulation) + DOM renderer
   ========================================================= */
'use strict';

class Battle {
  constructor(left, right, opts = {}) {
    this.t = 0; this.events = []; this.pending = []; this.over = false; this.winner = null;
    this.timeLimit = opts.timeLimit || 90;
    this.bossMode = !!opts.bossMode;
    this.dmgDealt = [0, 0];
    this.units = [];
    left.forEach((u, i) => u && this.units.push(this.mk(u, 0, i)));
    right.forEach((u, i) => u && this.units.push(this.mk(u, 1, i)));
  }
  mk(spec, side, slot) {
    const sp = new Set(spec.specials || []);
    const maxHp = spec.maxHpOverride || spec.hp;
    return Object.assign({}, spec, {
      uid: side * 10 + slot, side, slot, maxHp, hp: spec.hp, energy: 0, gauge: Math.random() * 50,
      alive: true, shield: sp.has('bulwark') ? maxHp * 0.25 : 0, stunUntil: 0, burns: [], atkBuff: 0, atkBuffUntil: 0,
      defDownUntil: 0, frenzy: 0, sp,
    });
  }
  alive(side) { return this.units.filter(u => u.alive && u.side === side); }
  byUid(uid) { return this.units.find(u => u.uid === uid); }
  emit(e) { this.events.push(e); }
  later(delay, fn) { this.pending.push({ at: this.t + delay, fn }); }

  update(dt) {
    if (this.over) return;
    this.t += dt;
    if (this.pending.length) {
      const due = this.pending.filter(p => p.at <= this.t);
      this.pending = this.pending.filter(p => p.at > this.t);
      due.forEach(p => p.fn());
    }
    for (const u of this.units) {
      if (!u.alive) continue;
      if (u.sp.has('regen') && u.hp < u.maxHp) u.hp = Math.min(u.maxHp, u.hp + u.maxHp * 0.02 * dt);
      if (u.burns.length) {
        for (const b of u.burns) {
          if (this.t >= b.next) { b.next += 1; this.applyRaw(b.src, u, b.dps, 'burn'); }
        }
        u.burns = u.burns.filter(b => b.until > this.t && u.alive);
      }
      if (!u.alive) continue;
      if (this.t < u.stunUntil) continue;
      u.gauge += u.spd * dt;
      if (u.gauge >= 100) { u.gauge -= 100; this.act(u); }
    }
    const l = this.alive(0).length, r = this.alive(1).length;
    if (!l || !r) this.finish(l ? 0 : 1);
    else if (this.t >= this.timeLimit) this.finish(this.bossMode ? 'time' : 1);
  }
  finish(w) {
    if (this.over) return;
    this.over = true; this.winner = w;
    this.emit({ type: 'end', winner: w });
  }
  simulate(maxT = 200) { let n = 0; while (!this.over && n < maxT * 20) { this.update(0.05); n++; } this.events = []; }

  pick(u, mode) {
    const foes = this.alive(1 - u.side);
    if (!foes.length) return null;
    const front = foes.filter(f => f.slot < 2), back = foes.filter(f => f.slot >= 2);
    switch (mode) {
      case 'front': return U.pick(front.length ? front : back);
      case 'back': return U.pick(back.length ? back : front);
      case 'snipe': return foes.reduce((a, b) => (b.hp / b.maxHp < a.hp / a.maxHp ? b : a));
      default: return U.pick(foes);
    }
  }
  basicMode(u) { return { warrior: 'front', assassin: 'back', archer: 'snipe', mage: 'random', support: 'front' }[u.cls] || 'front'; }
  gainEnergy(u, n) { u.energy = Math.min(100, u.energy + n * (u.sp.has('focus') ? 1.35 : 1)); }

  act(u) {
    if (u.energy >= 100 && u.skill) { u.energy = 0; this.cast(u); return; }
    const t = this.pick(u, this.basicMode(u));
    if (!t) return;
    this.emit({ type: 'attack', src: u.uid, tgt: t.uid, ranged: u.ranged });
    this.gainEnergy(u, 25);
    if (u.sp.has('frenzy')) u.frenzy = Math.min(10, u.frenzy + 1);
    this.later(0.22, () => this.damage(u, t, 1, {}));
  }

  cast(u) {
    const sk = u.skill, ex = sk;
    let targets = [];
    const allies = this.alive(u.side);
    switch (sk.type) {
      case 'strike': targets = [this.pick(u, 'front')]; break;
      case 'back': targets = [this.pick(u, 'back')]; break;
      case 'snipe': targets = [this.pick(u, 'snipe')]; break;
      case 'aoe': targets = this.alive(1 - u.side); break;
      case 'multi': targets = []; for (let i = 0; i < (sk.hits || 2); i++) targets.push(this.pick(u, 'random')); break;
      case 'heal': targets = [allies.reduce((a, b) => (b.hp / b.maxHp < a.hp / a.maxHp ? b : a))]; break;
      case 'healAll': case 'shield': targets = allies; break;
    }
    targets = targets.filter(Boolean);
    this.emit({ type: 'skill', src: u.uid, name: sk.name, skillType: sk.type, targets: targets.map(t => t.uid) });
    const isSupport = sk.type === 'heal' || sk.type === 'healAll' || sk.type === 'shield';
    const pow = u.atk * (1 + (this.t < u.atkBuffUntil ? u.atkBuff : 0));
    if (isSupport) {
      this.later(0.3, () => {
        for (const a of targets) {
          if (!a.alive) continue;
          if (sk.type === 'shield') { const v = pow * sk.mult; a.shield += v; this.emit({ type: 'shield', tgt: a.uid, amt: v }); }
          else this.heal(a, pow * sk.mult);
          if (sk.shieldAll) { const v = pow * sk.shieldAll; a.shield += v; this.emit({ type: 'shield', tgt: a.uid, amt: v }); }
          if (sk.buffAtk) { a.atkBuff = sk.buffAtk.pct / 100; a.atkBuffUntil = this.t + sk.buffAtk.dur; this.emit({ type: 'buff', tgt: a.uid }); }
        }
      });
    } else {
      targets.forEach((t, i) => {
        this.later(0.32 + (sk.type === 'multi' ? i * 0.12 : 0), () => {
          if (!t.alive) { const alt = this.pick(u, 'random'); if (!alt || sk.type !== 'multi') return; t = alt; }
          this.damage(u, t, sk.mult, ex);
        });
      });
    }
  }

  heal(a, amt) {
    if (!a.alive) return;
    const v = Math.min(a.maxHp - a.hp, amt);
    a.hp += v;
    this.emit({ type: 'heal', tgt: a.uid, amt: Math.max(0, v) });
  }

  damage(src, tgt, mult, ex) {
    if (!tgt || !tgt.alive) return;
    if (tgt.sp.has('evasion') && U.chance(0.12)) { this.emit({ type: 'dodge', tgt: tgt.uid }); return; }
    const buff = this.t < src.atkBuffUntil ? src.atkBuff : 0;
    const atk = src.atk * (1 + buff) * (1 + src.frenzy * 0.04);
    const def = tgt.def * (this.t < tgt.defDownUntil ? 0.7 : 1);
    const red = def / (def + src.atk * 1.2);
    let dmg = atk * mult * (1 - red) * U.rand(0.92, 1.08);
    const crit = U.chance(src.crit / 100);
    if (crit) dmg *= 1 + src.critDmg / 100;
    if (tgt.hp / tgt.maxHp < 0.3) { if (src.sp.has('execute')) dmg *= 1.6; if (ex.execute) dmg *= 2; }
    if (tgt.isBoss && this.bossMode) dmg *= 1; // hook for boss modifiers
    this.applyRaw(src, tgt, dmg, crit ? 'crit' : 'hit');
    // energy & procs
    this.gainEnergy(tgt, 10);
    const ls = (ex.lifesteal || 0) + (src.sp.has('lifesteal') ? 0.12 : 0);
    if (ls > 0 && src.alive) this.heal(src, dmg * ls);
    if (tgt.sp.has('thorns') && src.alive) this.applyRaw(tgt, src, dmg * 0.2, 'thorns');
    if (!tgt.alive) return;
    const stunDur = d => (tgt.isBoss ? d * 0.5 : d);
    if (ex.stun && U.chance(ex.stun.chance)) { tgt.stunUntil = Math.max(tgt.stunUntil, this.t + stunDur(ex.stun.dur)); this.emit({ type: 'stun', tgt: tgt.uid }); }
    else if (src.sp.has('stun') && U.chance(0.1)) { tgt.stunUntil = Math.max(tgt.stunUntil, this.t + stunDur(1.2)); this.emit({ type: 'stun', tgt: tgt.uid }); }
    if (ex.burn) tgt.burns.push({ src, dps: src.atk * ex.burn, until: this.t + 3.01, next: this.t + 1 });
    if (src.sp.has('burn') && U.chance(0.25)) tgt.burns.push({ src, dps: src.atk * 0.3, until: this.t + 3.01, next: this.t + 1 });
    if (ex.defDown) tgt.defDownUntil = this.t + 4;
  }

  applyRaw(src, tgt, dmg, kind) {
    if (!tgt.alive) return;
    let absorbed = 0;
    if (tgt.shield > 0) { absorbed = Math.min(tgt.shield, dmg); tgt.shield -= absorbed; dmg -= absorbed; }
    tgt.hp -= dmg;
    this.dmgDealt[src.side] += dmg + absorbed;
    this.emit({ type: 'dmg', tgt: tgt.uid, amt: dmg + absorbed, kind, src: src.uid });
    if (tgt.hp <= 0) {
      tgt.hp = 0; tgt.alive = false; tgt.burns = [];
      this.emit({ type: 'death', tgt: tgt.uid });
    }
  }
}

/* ---------------- Renderer ---------------- */
const SLOT_POS = [
  [34, 40], [34, 74], [13, 26], [13, 57], [13, 86],
];
class BattleView {
  constructor(container, battle, opts = {}) {
    this.c = container; this.b = battle; this.opts = opts;
    this.speed = opts.speed || 1; this.running = true; this.ended = false; this.last = 0;
    this.build();
    this.loop = this.loop.bind(this);
    this.raf = requestAnimationFrame(this.loop);
  }
  pos(u) {
    const p = SLOT_POS[u.slot];
    return u.side === 0 ? [p[0], p[1]] : [100 - p[0], p[1]];
  }
  build() {
    const theme = this.opts.theme || 0;
    this.c.innerHTML = `<div class="bstage theme-${theme % 6}${this.opts.idle ? ' idle' : ''}">
      <div class="bg-sky"></div><div class="bg-hills"></div><div class="bg-ground"></div><div class="bg-fog"></div>
      <div class="units"></div><div class="fx"></div><div class="banner"></div></div>`;
    this.stage = this.c.firstElementChild;
    this.unitsEl = this.stage.querySelector('.units');
    this.fx = this.stage.querySelector('.fx');
    this.banner = this.stage.querySelector('.banner');
    this.els = {};
    for (const u of this.b.units) {
      const [x, y] = this.pos(u);
      const d = document.createElement('div');
      d.className = `unit side${u.side}${u.isBoss ? ' boss' : ''}${u.rarity != null ? ' r' + u.rarity : ''}`;
      d.style.left = x + '%'; d.style.top = y + '%'; d.style.zIndex = Math.round(y);
      d.innerHTML = `<div class="u-body"><div class="u-sprite">${u.sprite}</div></div>
        <div class="u-bars"><div class="hpbar"><i class="sh"></i><i class="hp"></i></div><div class="enbar"><i></i></div></div>
        ${u.isBoss ? `<div class="u-name">${U.esc(u.name)}</div>` : ''}<div class="u-status"></div>`;
      this.unitsEl.appendChild(d);
      this.els[u.uid] = { root: d, body: d.querySelector('.u-body'), sprite: d.querySelector('.u-sprite'), hp: d.querySelector('.hp'), sh: d.querySelector('.sh'), en: d.querySelector('.enbar i'), st: d.querySelector('.u-status') };
    }
    this.syncBars();
  }
  px(u) {
    const [x, y] = this.pos(u);
    return [x / 100 * this.stage.clientWidth, y / 100 * this.stage.clientHeight];
  }
  setSpeed(s) { this.speed = s; }
  stop() { this.running = false; cancelAnimationFrame(this.raf); }
  resume() { if (this.finished || this.running) return; this.running = true; this.last = 0; this.raf = requestAnimationFrame(this.loop); }
  skip() {
    // simulate instantly to the end and show final state
    this.b.simulate();
    this.syncBars();
    this.b.units.forEach(u => { if (!u.alive) this.els[u.uid].root.classList.add('dead'); });
    this.finish();
  }
  loop(ts) {
    if (!this.running) return;
    const dt = this.last ? Math.min(0.05, (ts - this.last) / 1000) : 0;
    this.last = ts;
    if (!document.hidden && !this.b.over) this.b.update(dt * this.speed);
    this.drain();
    this.syncBars();
    if (this.b.over && !this.ended) {
      this.ended = true;
      setTimeout(() => this.finish(), this.opts.idle ? 500 : 900 / Math.max(1, this.speed * 0.6));
    }
    this.raf = requestAnimationFrame(this.loop);
  }
  finish() {
    if (this.finished) return;
    this.finished = true; this.ended = true;
    this.stop();
    if (this.opts.onEnd) this.opts.onEnd(this.b);
  }
  syncBars() {
    for (const u of this.b.units) {
      const e = this.els[u.uid];
      e.hp.style.width = (u.hp / u.maxHp * 100).toFixed(1) + '%';
      e.sh.style.width = Math.min(100, (u.hp + u.shield) / u.maxHp * 100).toFixed(1) + '%';
      e.en.style.width = u.energy + '%';
      const stun = this.b.t < u.stunUntil && u.alive, burn = u.burns.length > 0, buff = this.b.t < u.atkBuffUntil;
      const key = `${stun}${burn}${buff}`;
      if (e.stKey !== key) { e.stKey = key; e.st.innerHTML = (stun ? '💫' : '') + (burn ? '🔥' : '') + (buff ? '⬆️' : ''); e.root.classList.toggle('stunned', stun); }
      if (u.energy >= 100 && u.alive) e.root.classList.add('ready'); else e.root.classList.remove('ready');
    }
  }
  float(u, text, cls) {
    const [x, y] = this.px(u);
    const d = document.createElement('div');
    d.className = 'fnum ' + cls; d.textContent = text;
    d.style.left = (x + U.rand(-12, 12)) + 'px'; d.style.top = (y - 30) + 'px';
    this.fx.appendChild(d);
    const dur = 900 / Math.min(2, this.speed);
    d.animate([{ transform: 'translate(-50%,0) scale(.6)', opacity: 0 }, { transform: 'translate(-50%,-14px) scale(1.15)', opacity: 1, offset: 0.15 }, { transform: 'translate(-50%,-44px) scale(1)', opacity: 0 }], { duration: dur, easing: 'ease-out' }).onfinish = () => d.remove();
  }
  burst(u, cls) {
    const [x, y] = this.px(u);
    const d = document.createElement('div');
    d.className = 'burst ' + cls; d.style.left = x + 'px'; d.style.top = y + 'px';
    this.fx.appendChild(d);
    d.animate([{ transform: 'translate(-50%,-50%) scale(.2)', opacity: 1 }, { transform: 'translate(-50%,-50%) scale(1.6)', opacity: 0 }], { duration: 420 / Math.min(2, this.speed), easing: 'ease-out' }).onfinish = () => d.remove();
  }
  projectile(src, tgt, cls) {
    const [x1, y1] = this.px(src), [x2, y2] = this.px(tgt);
    const d = document.createElement('div');
    d.className = 'proj ' + cls;
    this.fx.appendChild(d);
    const ang = Math.atan2(y2 - y1, x2 - x1) * 180 / Math.PI;
    d.animate([{ transform: `translate(${x1}px,${y1 - 10}px) rotate(${ang}deg)` }, { transform: `translate(${x2}px,${y2 - 10}px) rotate(${ang}deg)` }], { duration: 210 / this.speed, easing: 'linear' }).onfinish = () => d.remove();
  }
  shake(strength = 1) {
    if (this.opts.idle) return;
    this.stage.animate([{ transform: 'translate(0,0)' }, { transform: `translate(${-4 * strength}px,${2 * strength}px)` }, { transform: `translate(${4 * strength}px,${-2 * strength}px)` }, { transform: 'translate(0,0)' }], { duration: 220 });
  }
  projClass(u) {
    if (u.isMonster) return u.kind === 'skeleton' ? 'arrow' : u.kind === 'demon' || u.kind === 'dragon' ? 'fire' : 'shadow';
    return { archer: 'arrow', mage: 'orb', support: 'holy' }[u.cls] || 'orb';
  }
  drain() {
    const evs = this.b.events; if (!evs.length) return;
    this.b.events = [];
    const quiet = this.opts.idle;
    for (const e of evs) {
      switch (e.type) {
        case 'attack': {
          const s = this.b.byUid(e.src), t = this.b.byUid(e.tgt); if (!s || !t) break;
          const el = this.els[s.uid].body;
          const dir = s.side === 0 ? 1 : -1;
          if (e.ranged) {
            el.animate([{ transform: 'translateX(0)' }, { transform: `translateX(${-6 * dir}px)` }, { transform: 'translateX(0)' }], { duration: 220 / this.speed });
            this.projectile(s, t, this.projClass(s));
          } else {
            const [x1, y1] = this.px(s), [x2, y2] = this.px(t);
            const dx = (x2 - x1) * 0.72, dy = (y2 - y1) * 0.72;
            el.animate([{ transform: 'translate(0,0)' }, { transform: `translate(${dx}px,${dy}px)`, offset: 0.45 }, { transform: 'translate(0,0)' }], { duration: 420 / this.speed, easing: 'ease-in-out' });
          }
          break;
        }
        case 'skill': {
          const s = this.b.byUid(e.src); if (!s) break;
          const el = this.els[s.uid];
          el.root.classList.add('casting'); setTimeout(() => el.root.classList.remove('casting'), 600 / this.speed);
          if (!quiet) {
            this.banner.innerHTML = `<span class="side${s.side}">${U.esc(s.name)}</span> <b>${U.esc(e.name)}</b>`;
            this.banner.classList.remove('show'); void this.banner.offsetWidth; this.banner.classList.add('show');
            SFX.play('skill');
          }
          const kind = e.skillType === 'heal' || e.skillType === 'healAll' ? 'holy' : e.skillType === 'shield' ? 'shieldfx' : (s.cls === 'mage' ? 'arcane' : s.cls === 'assassin' ? 'shadow' : 'slash');
          e.targets.forEach((uid, i) => {
            const t = this.b.byUid(uid); if (!t) return;
            setTimeout(() => this.burst(t, kind), (e.skillType === 'multi' ? i * 120 : 0) / this.speed + 150 / this.speed);
            if (s.ranged && e.skillType !== 'aoe' && !['heal', 'healAll', 'shield'].includes(e.skillType)) setTimeout(() => this.projectile(s, t, this.projClass(s)), i * 120 / this.speed);
          });
          if (e.skillType === 'aoe') { this.shake(1.5); this.stage.classList.add('flash' + s.side); setTimeout(() => this.stage.classList.remove('flash' + s.side), 350); }
          if (!s.ranged && ['strike', 'back', 'snipe'].includes(e.skillType) && e.targets[0] != null) {
            const t = this.b.byUid(e.targets[0]);
            const [x1, y1] = this.px(s), [x2, y2] = this.px(t);
            el.body.animate([{ transform: 'translate(0,0) scale(1)' }, { transform: `translate(${(x2 - x1) * 0.75}px,${(y2 - y1) * 0.75}px) scale(1.15)`, offset: 0.5 }, { transform: 'translate(0,0) scale(1)' }], { duration: 520 / this.speed, easing: 'ease-in-out' });
          }
          break;
        }
        case 'dmg': {
          const t = this.b.byUid(e.tgt); if (!t) break;
          const el = this.els[t.uid];
          const cls = e.kind === 'crit' ? 'crit' : e.kind === 'burn' ? 'burnn' : e.kind === 'thorns' ? 'thorns' : (t.side === 0 ? 'dmg-me' : 'dmg');
          if (!quiet || e.kind === 'crit') this.float(t, (e.kind === 'crit' ? '💥' : '') + U.fmt(e.amt), cls);
          el.sprite.animate([{ filter: 'brightness(3) saturate(0)' }, { filter: 'brightness(1)' }], { duration: 160 });
          el.body.animate([{ transform: 'translateX(0)' }, { transform: `translateX(${t.side === 0 ? -5 : 5}px)` }, { transform: 'translateX(0)' }], { duration: 160 });
          if (e.kind === 'crit') { this.shake(1); if (!quiet) SFX.play('crit'); } else if (!quiet) SFX.play('hit');
          break;
        }
        case 'heal': { const t = this.b.byUid(e.tgt); if (t && e.amt > 1 && !quiet) { this.float(t, '+' + U.fmt(e.amt), 'heal'); SFX.play('heal'); } break; }
        case 'shield': { const t = this.b.byUid(e.tgt); if (t && !quiet) this.float(t, '🛡' + U.fmt(e.amt), 'shieldn'); break; }
        case 'dodge': { const t = this.b.byUid(e.tgt); if (t) this.float(t, 'MISS', 'miss'); break; }
        case 'stun': { const t = this.b.byUid(e.tgt); if (t && !quiet) this.float(t, 'STUN', 'stun'); break; }
        case 'buff': break;
        case 'death': {
          const t = this.b.byUid(e.tgt); if (!t) break;
          this.els[t.uid].root.classList.add('dead');
          this.burst(t, 'death');
          break;
        }
      }
    }
  }
}
