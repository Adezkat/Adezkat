/* =========================================================
   Utilities: RNG, formatting, DOM helpers, toasts, modals, sfx
   ========================================================= */
'use strict';

const U = {};

U.rand = (a, b) => a + Math.random() * (b - a);
U.randi = (a, b) => Math.floor(U.rand(a, b + 1));
U.pick = arr => arr[Math.floor(Math.random() * arr.length)];
U.clamp = (v, a, b) => Math.max(a, Math.min(b, v));
U.chance = p => Math.random() < p;
U.weighted = weights => {
  const total = weights.reduce((s, w) => s + w, 0);
  let r = Math.random() * total;
  for (let i = 0; i < weights.length; i++) { r -= weights[i]; if (r <= 0) return i; }
  return weights.length - 1;
};
/* Deterministic seeded RNG (mulberry32) for simulated players/guilds */
U.seeded = seed => {
  let a = seed >>> 0;
  return () => {
    a |= 0; a = a + 0x6D2B79F5 | 0;
    let t = Math.imul(a ^ a >>> 15, 1 | a);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
};
U.hash = str => { let h = 2166136261; for (const c of str) { h ^= c.charCodeAt(0); h = Math.imul(h, 16777619); } return h >>> 0; };

const SUFFIX = ['', 'K', 'M', 'B', 'T', 'aa', 'ab', 'ac', 'ad'];
U.fmt = n => {
  n = Math.floor(n || 0);
  if (Math.abs(n) < 10000) return n.toLocaleString('en-US');
  let i = 0; let v = n;
  while (Math.abs(v) >= 1000 && i < SUFFIX.length - 1) { v /= 1000; i++; }
  return (v >= 100 ? v.toFixed(0) : v >= 10 ? v.toFixed(1) : v.toFixed(2)) + SUFFIX[i];
};
U.fmtTime = sec => {
  sec = Math.max(0, Math.floor(sec));
  const d = Math.floor(sec / 86400), h = Math.floor(sec % 86400 / 3600), m = Math.floor(sec % 3600 / 60), s = sec % 60;
  if (d > 0) return `${d}d ${h}h`;
  if (h > 0) return `${h}h ${String(m).padStart(2, '0')}m`;
  return `${m}m ${String(s).padStart(2, '0')}s`;
};
U.esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
U.dayKey = (d = new Date()) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
U.weekKey = (d = new Date()) => {
  // Week starts Monday 00:00 local
  const x = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const dow = (x.getDay() + 6) % 7;
  x.setDate(x.getDate() - dow);
  return U.dayKey(x);
};
U.secsToNextDay = () => { const n = new Date(); const t = new Date(n.getFullYear(), n.getMonth(), n.getDate() + 1); return (t - n) / 1000; };
U.secsToNextWeek = () => {
  const n = new Date(); const dow = (n.getDay() + 6) % 7;
  const t = new Date(n.getFullYear(), n.getMonth(), n.getDate() + (7 - dow));
  return (t - n) / 1000;
};

U.$ = (sel, root = document) => root.querySelector(sel);
U.$$ = (sel, root = document) => [...root.querySelectorAll(sel)];

/* ---------- Rewards formatting ---------- */
U.rewardChips = (rew, cls = '') => Object.entries(rew).filter(([, v]) => v).map(([k, v]) => {
  if (RES[k]) return `<span class="chip ${cls}">${RES[k].icon} ${U.fmt(v)}</span>`;
  if (CONSUMABLES[k]) return `<span class="chip ${cls}">${CONSUMABLES[k].icon} ${CONSUMABLES[k].name} ×${v}</span>`;
  if (k.startsWith('shard:')) { const h = HERO_BY_ID[k.slice(6)]; return `<span class="chip ${cls}" style="color:${RARITIES[h.rarity].color}">🧩 ${h.name} ×${v}</span>`; }
  if (k.startsWith('art:')) { const a = ART_BY_ID[k.slice(4)]; return `<span class="chip ${cls}">${a.icon} ${a.name} frag ×${v}</span>`; }
  return `<span class="chip ${cls}">${k} ×${U.fmt(v)}</span>`;
}).join('');

/* ---------- Toasts ---------- */
U.toast = (msg, type = '') => {
  const root = document.getElementById('toast-root');
  const t = document.createElement('div');
  t.className = 'toast ' + type;
  t.innerHTML = msg;
  root.appendChild(t);
  while (root.children.length > 3) root.firstChild.remove();
  setTimeout(() => t.classList.add('out'), 1900);
  setTimeout(() => t.remove(), 2300);
};

/* ---------- Modals (stackable) ---------- */
U.modal = (html, opts = {}) => {
  const root = document.getElementById('modal-root');
  const wrap = document.createElement('div');
  wrap.className = 'modal-wrap' + (opts.full ? ' full' : '');
  wrap.innerHTML = `<div class="modal ${opts.cls || ''}">
      ${opts.title ? `<div class="modal-head"><h2>${opts.title}</h2>${opts.noClose ? '' : '<button class="x" data-act="closeModal">✕</button>'}</div>` : ''}
      <div class="modal-body">${html}</div></div>`;
  if (!opts.noClose) wrap.addEventListener('pointerdown', e => { if (e.target === wrap) U.closeModal(); });
  wrap._onClose = opts.onClose;
  wrap._render = opts.render;
  root.appendChild(wrap);
  SFX.play('open');
  return wrap;
};
U.closeModal = () => {
  const root = document.getElementById('modal-root');
  const top = root.lastElementChild;
  if (!top) return;
  top.remove();
  if (top._onClose) top._onClose();
};
U.closeAllModals = () => { const r = document.getElementById('modal-root'); while (r.lastElementChild) U.closeModal(); };
/* Re-render a modal that was opened with a render function */
U.refreshModals = () => {
  U.$$('#modal-root .modal-wrap').forEach(w => {
    if (w._render) {
      const body = w.querySelector('.modal-body');
      const st = body.scrollTop;
      body.innerHTML = w._render();
      body.scrollTop = st;
    }
  });
};
U.confirm = (msg, onYes, yesLabel = 'Confirm') => {
  U.modal(`<p class="center">${msg}</p><div class="row gap mt"><button class="btn ghost grow" data-act="closeModal">Cancel</button><button class="btn gold grow" data-act="confirmYes">${yesLabel}</button></div>`, { title: 'Confirm', cls: 'small' });
  U._confirmCb = onYes;
};

/* ---------- Sound (tiny WebAudio synth) ---------- */
const SFX = {
  ctx: null, enabled: true, last: {},
  init() { try { this.ctx = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { this.ctx = null; } },
  play(type) {
    if (!this.enabled) return;
    if (!this.ctx) this.init();
    if (!this.ctx) return;
    const now = performance.now();
    if (this.last[type] && now - this.last[type] < 45) return;
    this.last[type] = now;
    const c = this.ctx; if (c.state === 'suspended') c.resume();
    const t = c.currentTime;
    const tone = (freq, dur, type2 = 'sine', vol = 0.06, slide = 0, delay = 0) => {
      const o = c.createOscillator(), g = c.createGain();
      o.type = type2; o.frequency.setValueAtTime(freq, t + delay);
      if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(30, freq + slide), t + delay + dur);
      g.gain.setValueAtTime(vol, t + delay); g.gain.exponentialRampToValueAtTime(0.0001, t + delay + dur);
      o.connect(g); g.connect(c.destination); o.start(t + delay); o.stop(t + delay + dur + 0.02);
    };
    switch (type) {
      case 'click': tone(660, 0.05, 'triangle', 0.04); break;
      case 'open': tone(440, 0.08, 'triangle', 0.03, 220); break;
      case 'hit': tone(180, 0.08, 'square', 0.025, -100); break;
      case 'crit': tone(320, 0.12, 'sawtooth', 0.04, -200); break;
      case 'skill': tone(520, 0.2, 'sawtooth', 0.035, 500); break;
      case 'heal': tone(700, 0.18, 'sine', 0.04, 300); break;
      case 'forge': tone(900, 0.06, 'square', 0.05); tone(1400, 0.25, 'triangle', 0.03, -600, 0.05); break;
      case 'win': [523, 659, 784, 1046].forEach((f, i) => tone(f, 0.25, 'triangle', 0.05, 0, i * 0.11)); break;
      case 'lose': [392, 330, 262].forEach((f, i) => tone(f, 0.3, 'triangle', 0.05, 0, i * 0.15)); break;
      case 'reward': tone(880, 0.1, 'triangle', 0.05); tone(1320, 0.18, 'triangle', 0.04, 0, 0.08); break;
      case 'legend': [660, 880, 1100, 1320, 1760].forEach((f, i) => tone(f, 0.3, 'sine', 0.05, 0, i * 0.08)); break;
      case 'error': tone(160, 0.15, 'square', 0.04); break;
    }
  },
};
