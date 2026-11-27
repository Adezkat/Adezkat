/* =========================================================
   Procedural SVG art: heroes, monsters, equipment, emblems
   All sprites face RIGHT; enemy side is mirrored with CSS.
   ========================================================= */
'use strict';

const ART = {};

ART.shade = (hex, amt) => {
  // amt -1..1 ; negative darkens
  let c = hex.replace('#', '');
  if (c.length === 3) c = c.split('').map(x => x + x).join('');
  const n = parseInt(c, 16);
  let r = n >> 16, g = n >> 8 & 255, b = n & 255;
  const f = amt < 0 ? 0 : 255, p = Math.abs(amt);
  r = Math.round((f - r) * p + r); g = Math.round((f - g) * p + g); b = Math.round((f - b) * p + b);
  return '#' + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
};

const cache = new Map();

/* ---------------- HEROES ---------------- */
ART.hero = (heroId) => {
  const key = 'h:' + heroId;
  if (cache.has(key)) return cache.get(key);
  const h = HERO_BY_ID[heroId];
  const p = h.pal, r = h.rarity, id = heroId;
  const main = p.main, dk = ART.shade(main, -0.45), lt = ART.shade(main, 0.25), acc = p.acc;
  const rc = RARITIES[r].color;
  let defs = `<defs>
    <linearGradient id="bd-${id}" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${lt}"/><stop offset="1" stop-color="${dk}"/></linearGradient>
    <linearGradient id="cp-${id}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${ART.shade(acc, -0.35)}"/><stop offset="1" stop-color="${ART.shade(acc, -0.75)}"/></linearGradient>
    <radialGradient id="au-${id}"><stop offset="0" stop-color="${rc}" stop-opacity=".55"/><stop offset="1" stop-color="${rc}" stop-opacity="0"/></radialGradient>
    <radialGradient id="gl-${id}"><stop offset="0" stop-color="#fff"/><stop offset=".35" stop-color="${acc}"/><stop offset="1" stop-color="${acc}" stop-opacity="0"/></radialGradient>
  </defs>`;
  let s = defs;
  if (r >= 2) s += `<circle cx="50" cy="62" r="${40 + r * 3}" fill="url(#au-${id})"/>`;
  s += `<ellipse cx="50" cy="113" rx="26" ry="5" fill="#000" opacity=".45"/>`;
  const isRobe = h.cls === 'mage' || h.cls === 'support';
  // cape
  s += `<path d="M37 48 Q30 80 24 110 L74 110 Q68 80 63 48 Z" fill="url(#cp-${id})"/>`;
  // quiver for archers (behind)
  if (h.cls === 'archer') s += `<rect x="30" y="44" width="8" height="30" rx="3" fill="#5a3a1a" transform="rotate(-20 34 59)"/><g stroke="${acc}" stroke-width="2"><line x1="28" y1="44" x2="24" y2="36"/><line x1="32" y1="43" x2="30" y2="34"/></g>`;
  // legs
  if (!isRobe) {
    s += `<rect x="40" y="84" width="8" height="24" rx="2" fill="${dk}"/><rect x="52" y="84" width="8" height="24" rx="2" fill="${dk}"/>
          <path d="M38 102 h11 v7 h-13 z" fill="${ART.shade(dk, -0.4)}"/><path d="M51 102 h11 l3 7 h-14 z" fill="${ART.shade(dk, -0.4)}"/>`;
  }
  // torso / robe
  if (isRobe) {
    s += `<path d="M37 48 Q50 43 63 48 L70 109 L30 109 Z" fill="url(#bd-${id})"/>
          <path d="M50 50 L50 109" stroke="${acc}" stroke-width="2" opacity=".7"/>
          <path d="M30 109 L70 109 L68 103 L32 103 Z" fill="${acc}" opacity=".8"/>`;
  } else {
    s += `<path d="M36 48 Q50 43 64 48 L62 88 L38 88 Z" fill="url(#bd-${id})"/>`;
  }
  s += `<rect x="37" y="76" width="26" height="5" rx="1" fill="${ART.shade(acc, -0.3)}"/><rect x="47" y="75" width="6" height="7" rx="1" fill="${acc}"/>
        <path d="M50 54 l4 6 -4 6 -4 -6z" fill="${acc}" opacity=".9"/>`;
  // back arm
  s += `<path d="M37 50 Q30 62 33 74" stroke="${dk}" stroke-width="7" stroke-linecap="round" fill="none"/>`;
  // shoulders
  if (h.cls === 'warrior') {
    s += `<ellipse cx="36" cy="50" rx="9" ry="7" fill="${lt}" stroke="${acc}" stroke-width="1.5"/><ellipse cx="64" cy="50" rx="9" ry="7" fill="${lt}" stroke="${acc}" stroke-width="1.5"/>`;
  }
  // head
  s += `<circle cx="51" cy="35" r="11.5" fill="${p.skin}"/>`;
  // hair base
  if (h.cls === 'support') {
    s += `<path d="M39 35 Q38 20 51 21 Q64 21 63 33 Q60 26 51 27 Q44 28 43 40 Q42 56 36 62 Q36 48 39 35Z" fill="${p.hair}"/>`;
  } else if (h.cls !== 'assassin') {
    s += `<path d="M40 33 Q40 22 51 22 Q62 22 62 31 Q56 27 48 29 Q43 31 42 40Z" fill="${p.hair}"/>`;
  }
  // eyes (glow)
  s += `<ellipse cx="55" cy="35" rx="2" ry="1.6" fill="${p.eye}"/><ellipse cx="60.5" cy="35" rx="1.7" ry="1.5" fill="${p.eye}"/>
        <circle cx="57.5" cy="35" r="6" fill="url(#gl-${id})" opacity=".35"/>`;
  // headgear
  switch (h.cls) {
    case 'warrior':
      s += `<path d="M39 34 Q39 20 51 20 Q63 20 63 34 L63 37 L58 37 L58 31 L44 31 L44 40 L39 40 Z" fill="${lt}" stroke="${ART.shade(lt, -0.4)}" stroke-width="1"/>
            <rect x="55" y="30" width="2.5" height="10" fill="${ART.shade(lt, -0.3)}"/>`;
      if (r >= 2) s += `<path d="M42 24 Q34 14 36 6 Q40 16 46 21Z" fill="${acc}"/><path d="M58 22 Q64 12 70 10 Q64 18 61 25Z" fill="${acc}"/>`;
      else s += `<path d="M47 20 Q51 10 55 20" fill="${acc}"/>`;
      break;
    case 'assassin':
      s += `<path d="M37 38 Q36 18 51 18 Q66 19 64 34 L63 30 Q57 24 50 26 Q44 28 44 44 L38 46Z" fill="${main}" stroke="${ART.shade(main, -0.5)}" stroke-width="1"/>
            <path d="M50 38 Q57 43 63 38 L63 44 Q56 48 50 44Z" fill="${ART.shade(main, -0.3)}"/>`;
      break;
    case 'mage':
      s += `<path d="M34 28 Q51 22 70 28 Q62 31 51 30 Q40 31 34 28Z" fill="${dk}"/>
            <path d="M40 27 Q46 10 58 2 Q55 14 63 27Z" fill="url(#bd-${id})"/><circle cx="58" cy="3" r="2.5" fill="${acc}"/>
            <path d="M42 25 Q51 22 61 25" stroke="${acc}" stroke-width="2" fill="none"/>`;
      break;
    case 'archer':
      s += `<path d="M38 36 Q37 20 51 19 Q64 19 63 31 Q57 25 49 27 Q44 29 43 42Z" fill="${main}"/>
            <path d="M43 22 Q34 12 30 14 Q36 18 40 26Z" fill="${acc}"/>`;
      break;
    case 'support':
      s += `<ellipse cx="51" cy="15" rx="12" ry="3.2" fill="none" stroke="${acc}" stroke-width="2.5"/>
            <ellipse cx="51" cy="15" rx="12" ry="3.2" fill="none" stroke="#fff" stroke-width="0.8" opacity=".8"/>`;
      break;
  }
  // front arm + weapon
  const arm = `<path d="M63 50 Q72 58 72 68" stroke="${dk}" stroke-width="7" stroke-linecap="round" fill="none"/><circle cx="72" cy="69" r="3.6" fill="${p.skin}"/>`;
  switch (h.cls) {
    case 'warrior': {
      const big = r >= 2;
      s += `<g transform="rotate(${big ? 18 : 10} 72 69)">
          <path d="M70 ${big ? 4 : 18} L74 ${big ? 4 : 18} L75.5 ${big ? 10 : 22} L75 66 L69 66 L68.5 ${big ? 10 : 22} Z" fill="#dfe6ee" stroke="#7a8696" stroke-width="1"/>
          <line x1="72" y1="${big ? 10 : 22}" x2="72" y2="64" stroke="${acc}" stroke-width="1" opacity=".8"/>
          <rect x="63" y="64" width="18" height="4" rx="2" fill="${acc}"/><rect x="70" y="67" width="4" height="11" fill="#4a2c14"/><circle cx="72" cy="79" r="2.5" fill="${acc}"/></g>`;
      s += arm;
      break;
    }
    case 'assassin':
      s += arm + `<g transform="rotate(-35 72 69)"><path d="M70 68 L72 44 L74 68Z" fill="#e8e8f0" stroke="${acc}" stroke-width="1"/><rect x="68" y="67" width="8" height="2.5" fill="${acc}"/><rect x="71" y="69" width="2" height="6" fill="#222"/></g>
             <g transform="rotate(30 33 72)"><path d="M31 72 L33 52 L35 72Z" fill="#e8e8f0" stroke="${acc}" stroke-width="1"/><rect x="29" y="71" width="8" height="2" fill="${acc}"/></g>`;
      break;
    case 'mage':
      s += `<line x1="76" y1="22" x2="72" y2="110" stroke="#4a2c14" stroke-width="3.5" stroke-linecap="round"/>
            <path d="M70 26 Q76 12 82 26" stroke="${acc}" stroke-width="2" fill="none"/>
            <circle cx="76" cy="19" r="12" fill="url(#gl-${id})"/><circle cx="76" cy="19" r="5" fill="#fff" opacity=".9"/>` + arm;
      break;
    case 'archer':
      s += `<path d="M76 30 Q92 66 76 102" stroke="#5a3414" stroke-width="3.5" fill="none" stroke-linecap="round"/>
            <path d="M76 30 Q92 66 76 102" stroke="${acc}" stroke-width="1" fill="none" opacity=".7"/>
            <line x1="76" y1="30" x2="76" y2="102" stroke="#eee" stroke-width=".8"/>
            <line x1="62" y1="66" x2="90" y2="66" stroke="#c9a878" stroke-width="1.5"/><path d="M90 63 L95 66 L90 69Z" fill="${acc}"/>` + arm;
      break;
    case 'support':
      s += `<line x1="76" y1="24" x2="74" y2="110" stroke="#c9a060" stroke-width="3" stroke-linecap="round"/>
            <circle cx="76" cy="20" r="11" fill="url(#gl-${id})"/>
            <g stroke="${acc}" stroke-width="1.6">${[0, 45, 90, 135].map(a => `<line x1="76" y1="12" x2="76" y2="28" transform="rotate(${a} 76 20)"/>`).join('')}</g>
            <circle cx="76" cy="20" r="4" fill="#fff"/>` + arm;
      break;
  }
  if (r === 4) {
    s += `<g fill="${acc}">${[[20, 30], [82, 50], [26, 82], [86, 92], [14, 58]].map(([x, y], i) => `<circle cx="${x}" cy="${y}" r="${1 + (i % 2)}" class="spark" style="animation-delay:${i * 0.4}s"/>`).join('')}</g>`;
  }
  const svg = `<svg viewBox="0 0 100 120" class="sprite" xmlns="http://www.w3.org/2000/svg">${s}</svg>`;
  cache.set(key, svg);
  return svg;
};

/* ---------------- MONSTERS ---------------- */
ART.monster = (kind, col, boss = false) => {
  const key = `m:${kind}:${col}:${boss}`;
  if (cache.has(key)) return cache.get(key);
  const c = col, dk = ART.shade(c, -0.5), lt = ART.shade(c, 0.3);
  const id = (kind + c + boss).replace(/[^a-z0-9]/gi, '');
  let s = `<defs><linearGradient id="mg${id}" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${lt}"/><stop offset="1" stop-color="${dk}"/></linearGradient>
    <radialGradient id="ma${id}"><stop offset="0" stop-color="#ff3030" stop-opacity=".5"/><stop offset="1" stop-color="#ff3030" stop-opacity="0"/></radialGradient></defs>`;
  if (boss) s += `<circle cx="50" cy="64" r="50" fill="url(#ma${id})"/>`;
  s += `<ellipse cx="50" cy="113" rx="28" ry="5" fill="#000" opacity=".45"/>`;
  const G = `url(#mg${id})`;
  const eyes = (x, y, col2 = '#ff3a2a', sz = 2.4) => `<circle cx="${x}" cy="${y}" r="${sz}" fill="${col2}"/><circle cx="${x + 6}" cy="${y}" r="${sz * 0.85}" fill="${col2}"/><circle cx="${x + 3}" cy="${y}" r="${sz * 3}" fill="${col2}" opacity=".18"/>`;
  switch (kind) {
    case 'slime':
      s += `<path d="M20 110 Q16 70 50 54 Q84 70 80 110 Z" fill="${G}" opacity=".95"/><ellipse cx="40" cy="72" rx="7" ry="4" fill="#fff" opacity=".35"/>
            ${eyes(52, 84, '#111', 3.4)}<path d="M54 96 Q60 100 66 96" stroke="#111" stroke-width="2" fill="none"/>`;
      break;
    case 'goblin':
      s += `<rect x="40" y="84" width="7" height="24" fill="${dk}"/><rect x="52" y="84" width="7" height="24" fill="${dk}"/>
            <path d="M36 56 Q50 50 64 56 L62 90 L38 90Z" fill="#5a3a20"/><circle cx="51" cy="44" r="13" fill="${G}"/>
            <path d="M40 40 L22 30 L40 48Z" fill="${c}"/><path d="M62 40 L78 28 L62 48Z" fill="${c}"/>${eyes(51, 42, '#ffe02a', 2.2)}
            <path d="M52 52 l3 3 l3 -3" stroke="#fff" stroke-width="1.5" fill="none"/>
            <g transform="rotate(25 70 70)"><rect x="68" y="44" width="7" height="34" rx="3" fill="#6a4a2a"/><circle cx="71.5" cy="46" r="7" fill="#7a5a3a"/></g>`;
      break;
    case 'wolf':
      s += `<path d="M18 74 Q10 66 6 56 Q16 64 24 68Z" fill="${dk}"/><ellipse cx="44" cy="78" rx="26" ry="14" fill="${G}"/>
            <rect x="26" y="84" width="6" height="24" fill="${dk}"/><rect x="36" y="86" width="6" height="22" fill="${dk}"/><rect x="54" y="86" width="6" height="22" fill="${dk}"/><rect x="62" y="84" width="6" height="24" fill="${dk}"/>
            <path d="M60 70 Q66 52 80 56 L92 66 L84 72 Q74 78 62 80Z" fill="${G}"/><path d="M68 56 L70 44 L76 56Z" fill="${dk}"/>
            ${eyes(78, 61, '#ffcc2a', 1.8)}<path d="M84 70 l2 4 l2 -4 l2 4" stroke="#fff" stroke-width="1" fill="none"/>`;
      break;
    case 'skeleton':
      s += `<g stroke="${c}" stroke-width="4" stroke-linecap="round"><line x1="44" y1="86" x2="40" y2="108"/><line x1="56" y1="86" x2="60" y2="108"/><line x1="50" y1="52" x2="50" y2="86"/></g>
            <g stroke="${c}" stroke-width="2.5" fill="none">${[58, 64, 70, 76].map(y => `<path d="M40 ${y} Q50 ${y - 4} 60 ${y}"/>`).join('')}</g>
            <circle cx="51" cy="38" r="12" fill="${c}"/><rect x="45" y="46" width="12" height="7" rx="2" fill="${c}"/>${eyes(48, 37, '#7af0ff', 2.6)}
            <path d="M76 30 Q90 66 76 102" stroke="#6a5030" stroke-width="3" fill="none"/><line x1="76" y1="30" x2="76" y2="102" stroke="#ddd" stroke-width=".8"/>
            <line x1="56" y1="58" x2="74" y2="64" stroke="${c}" stroke-width="3"/>`;
      break;
    case 'orc':
      s += `<rect x="36" y="84" width="11" height="24" rx="2" fill="#3a2a1a"/><rect x="53" y="84" width="11" height="24" rx="2" fill="#3a2a1a"/>
            <path d="M28 54 Q50 44 72 54 L68 90 L32 90Z" fill="${G}"/><rect x="30" y="78" width="40" height="6" fill="#5a3a1a"/>
            <circle cx="51" cy="38" r="14" fill="${G}"/>${eyes(50, 35, '#ff4a1a', 2.4)}<path d="M50 46 l-2 -7 l4 4z M60 46 l2 -7 l-4 4z" fill="#fff"/>
            <g transform="rotate(20 76 64)"><rect x="74" y="28" width="5" height="52" fill="#5a3a1a"/><path d="M79 30 Q96 38 79 52Z" fill="#9aa4b0" stroke="#555"/></g>`;
      break;
    case 'spider':
      s += `<g stroke="${dk}" stroke-width="3" fill="none" stroke-linecap="round">${[-1, 1].map(d => [0, 1, 2, 3].map(i => `<path d="M50 80 Q${50 + d * (22 + i * 4)} ${62 + i * 6} ${50 + d * (30 + i * 4)} ${96 + i * 4}"/>`).join('')).join('')}</g>
            <ellipse cx="40" cy="78" rx="20" ry="16" fill="${G}"/><path d="M32 70 l8 4 l-8 4 M44 70 l-6 4 l6 4" stroke="#ff3a3a" stroke-width="1.5" fill="none"/>
            <circle cx="64" cy="80" r="11" fill="${G}"/>${eyes(63, 77, '#ff2a2a', 1.6)}<circle cx="66" cy="82" r="1.3" fill="#ff2a2a"/><circle cx="72" cy="82" r="1.3" fill="#ff2a2a"/>`;
      break;
    case 'wraith':
      s += `<g class="float"><path d="M30 50 Q50 14 72 50 L76 104 L68 96 L62 106 L54 96 L46 106 L40 96 L30 104 Z" fill="${G}" opacity=".85"/>
            <path d="M38 44 Q51 30 64 44 L62 58 Q51 64 40 58Z" fill="#05060a"/>${eyes(48, 49, '#aef6ff', 2.2)}
            <path d="M68 60 Q82 64 86 54" stroke="${lt}" stroke-width="4" fill="none" stroke-linecap="round"/><circle cx="87" cy="52" r="5" fill="#bff" opacity=".6"/></g>`;
      break;
    case 'golem':
      s += `<rect x="34" y="86" width="13" height="22" fill="${dk}"/><rect x="53" y="86" width="13" height="22" fill="${dk}"/>
            <path d="M26 52 L74 52 L70 90 L30 90Z" fill="${G}" stroke="${dk}" stroke-width="2"/><rect x="16" y="54" width="12" height="34" rx="4" fill="${G}"/><rect x="72" y="54" width="12" height="34" rx="4" fill="${G}"/>
            <rect x="38" y="28" width="26" height="22" rx="3" fill="${G}" stroke="${dk}" stroke-width="2"/>
            <rect x="46" y="36" width="14" height="4" fill="#5af0ff"/><path d="M44 62 L56 62 L50 76Z" fill="none" stroke="#5af0ff" stroke-width="2"/><circle cx="50" cy="68" r="10" fill="#5af0ff" opacity=".15"/>`;
      break;
    case 'demon':
      s += `<path d="M38 56 Q10 30 4 50 Q16 54 20 70 Q28 62 36 70Z" fill="${dk}"/><path d="M62 56 Q86 26 96 44 Q84 50 82 66 Q74 60 66 68Z" fill="${dk}"/>
            <rect x="40" y="84" width="8" height="24" fill="${dk}"/><rect x="52" y="84" width="8" height="24" fill="${dk}"/>
            <path d="M36 52 Q50 46 64 52 L62 88 L38 88Z" fill="${G}"/><circle cx="51" cy="38" r="12" fill="${G}"/>
            <path d="M42 30 Q36 16 44 10 Q42 20 47 27Z M60 30 Q66 16 58 10 Q60 20 55 27Z" fill="#2a1a1a"/>${eyes(50, 37, '#ffe02a', 2.2)}
            <circle cx="76" cy="66" r="7" fill="#ff8a2a" opacity=".9"/><circle cx="76" cy="66" r="12" fill="#ff5a1a" opacity=".3"/>`;
      break;
    case 'dragon':
      s += `<path d="M40 64 Q20 20 2 40 Q16 44 18 60 Q28 54 34 66Z" fill="${dk}"/>
            <path d="M14 96 Q4 100 2 88 Q10 94 22 88Z" fill="${dk}"/><ellipse cx="42" cy="84" rx="26" ry="16" fill="${G}"/>
            <rect x="28" y="92" width="8" height="16" fill="${dk}"/><rect x="50" y="92" width="8" height="16" fill="${dk}"/>
            <path d="M58 78 Q64 50 74 44 L82 48 Q70 60 68 82Z" fill="${G}"/><path d="M70 40 Q82 34 94 42 L92 50 Q84 52 74 50Z" fill="${G}"/>
            <path d="M74 38 L70 28 L80 36Z" fill="${dk}"/>${eyes(80, 42, '#ffe02a', 1.6)}<path d="M94 46 Q100 44 99 50" stroke="#ff8a2a" stroke-width="2" fill="none"/>`;
      break;
  }
  if (boss) s += `<path d="M40 14 L44 4 L50 12 L56 4 L60 14 Z" fill="#ffcf4a" stroke="#8a5a00" stroke-width="1"/>`;
  const svg = `<svg viewBox="0 0 100 120" class="sprite" xmlns="http://www.w3.org/2000/svg">${s}</svg>`;
  cache.set(key, svg);
  return svg;
};

/* ---------------- EQUIPMENT ICONS ---------------- */
ART.gear = (slot, rarity, variant = 0) => {
  const key = `g:${slot}:${rarity}:${variant}`;
  if (cache.has(key)) return cache.get(key);
  const rc = RARITIES[rarity].color, id = `${slot}${rarity}${variant}`;
  const metal = ['#b8c0cc', '#d8e4f0', '#c9b8ff', '#ffe08a', '#ffd0d8'][rarity];
  const gem = rc;
  let s = `<defs><linearGradient id="gm${id}" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#fff"/><stop offset=".3" stop-color="${metal}"/><stop offset="1" stop-color="${ART.shade(metal, -0.55)}"/></linearGradient>
    <radialGradient id="gg${id}"><stop offset="0" stop-color="#fff"/><stop offset=".5" stop-color="${gem}"/><stop offset="1" stop-color="${ART.shade(gem, -0.5)}"/></radialGradient></defs>`;
  const M = `url(#gm${id})`, GM = `url(#gg${id})`, ol = ART.shade(metal, -0.7);
  switch (slot) {
    case 'weapon':
      if (variant % 3 === 2) {
        s += `<g transform="rotate(40 32 32)"><rect x="30" y="16" width="4" height="44" fill="#6a4020"/><path d="M34 10 Q52 16 50 30 Q44 26 34 28Z" fill="${M}" stroke="${ol}"/><path d="M30 10 Q12 16 14 30 Q20 26 30 28Z" fill="${M}" stroke="${ol}"/><circle cx="32" cy="18" r="3.5" fill="${GM}"/></g>`;
      } else {
        s += `<g transform="rotate(45 32 32)"><path d="M29 6 L32 1 L35 6 L35 42 L29 42Z" fill="${M}" stroke="${ol}" stroke-width="1"/><line x1="32" y1="5" x2="32" y2="40" stroke="#fff" stroke-width=".8" opacity=".7"/>
          <path d="M20 42 Q32 ${variant % 3 ? 38 : 46} 44 42 L44 46 L20 46Z" fill="${gem}" stroke="${ol}"/><rect x="30" y="46" width="4" height="11" fill="#5a3418"/><circle cx="32" cy="59" r="3.5" fill="${GM}"/></g>`;
      }
      break;
    case 'helmet':
      s += `<path d="M12 40 Q12 10 32 10 Q52 10 52 40 L52 50 L42 50 L42 36 L22 36 L22 50 L12 50Z" fill="${M}" stroke="${ol}" stroke-width="1.5"/>
            <rect x="30" y="34" width="4" height="16" fill="${ART.shade(metal, -0.4)}"/><path d="M22 18 Q32 14 42 18" stroke="#fff" stroke-width="1.5" fill="none" opacity=".7"/>
            <circle cx="32" cy="24" r="4" fill="${GM}"/>${rarity >= 2 ? `<path d="M14 24 Q4 12 8 4 Q12 14 18 18Z M50 24 Q60 12 56 4 Q52 14 46 18Z" fill="${gem}"/>` : ''}`;
      break;
    case 'armor':
      s += `<path d="M16 14 L24 10 Q32 16 40 10 L48 14 L56 24 L50 30 L48 26 L48 54 Q32 60 16 54 L16 26 L14 30 L8 24Z" fill="${M}" stroke="${ol}" stroke-width="1.5"/>
            <path d="M32 18 L32 54" stroke="${ol}" stroke-width="1"/><path d="M20 34 Q32 38 44 34 M20 44 Q32 48 44 44" stroke="${ol}" stroke-width="1" fill="none"/>
            <circle cx="32" cy="26" r="4.5" fill="${GM}"/>`;
      break;
    case 'boots':
      s += `<path d="M20 8 L36 8 L36 40 L54 46 Q58 50 56 56 L16 56 L18 40Z" fill="${M}" stroke="${ol}" stroke-width="1.5"/>
            <rect x="18" y="8" width="20" height="7" rx="2" fill="${gem}" stroke="${ol}"/><path d="M16 52 L56 52" stroke="${ol}" stroke-width="2"/>
            <circle cx="28" cy="30" r="3.5" fill="${GM}"/>`;
      break;
    case 'accessory':
      if (variant % 2) {
        s += `<path d="M18 6 Q32 30 46 6" stroke="${metal}" stroke-width="2.5" fill="none"/><path d="M32 26 L44 38 L32 58 L20 38Z" fill="${M}" stroke="${ol}" stroke-width="1.5"/><circle cx="32" cy="40" r="7" fill="${GM}"/>`;
      } else {
        s += `<circle cx="32" cy="38" r="16" fill="none" stroke="${M}" stroke-width="7"/><circle cx="32" cy="38" r="16" fill="none" stroke="${ol}" stroke-width="1"/>
              <path d="M24 20 L32 10 L40 20 L32 26Z" fill="${GM}" stroke="${ol}"/>`;
      }
      break;
  }
  const svg = `<svg viewBox="0 0 64 64" class="gicon" xmlns="http://www.w3.org/2000/svg">${s}</svg>`;
  cache.set(key, svg);
  return svg;
};

/* Small decorative anvil for the forge screen */
ART.anvil = () => `<svg viewBox="0 0 200 140" class="anvil-svg" xmlns="http://www.w3.org/2000/svg">
  <defs><linearGradient id="anv" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#5a5f6e"/><stop offset="1" stop-color="#1b1d24"/></linearGradient>
  <radialGradient id="hot"><stop offset="0" stop-color="#fff6c0"/><stop offset=".4" stop-color="#ff9a2a"/><stop offset="1" stop-color="#ff3a00" stop-opacity="0"/></radialGradient></defs>
  <ellipse cx="100" cy="132" rx="80" ry="7" fill="#000" opacity=".5"/>
  <path d="M30 40 L170 40 Q172 56 150 60 L130 62 L126 90 L150 104 L150 124 L50 124 L50 104 L74 90 L70 62 L60 60 Q20 56 6 44 Q14 40 30 40Z" fill="url(#anv)" stroke="#0a0b0f" stroke-width="2"/>
  <path d="M34 42 L166 42" stroke="#9aa0b0" stroke-width="2"/>
  <ellipse cx="100" cy="36" rx="40" ry="10" fill="url(#hot)" class="anvil-glow"/>
  <rect x="78" y="31" width="44" height="7" rx="3" fill="#ffb24a"/>
</svg>`;

ART.chest = (open = false) => `<svg viewBox="0 0 100 80" class="chest-svg" xmlns="http://www.w3.org/2000/svg">
  <defs><linearGradient id="chw" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8a5a2a"/><stop offset="1" stop-color="#3a2410"/></linearGradient>
  <radialGradient id="chg"><stop offset="0" stop-color="#fff2a0"/><stop offset="1" stop-color="#ffb000" stop-opacity="0"/></radialGradient></defs>
  <ellipse cx="50" cy="40" rx="46" ry="34" fill="url(#chg)" opacity="${open ? 0.9 : 0.35}" class="chest-glow"/>
  <rect x="14" y="36" width="72" height="36" rx="4" fill="url(#chw)" stroke="#1a0f05" stroke-width="2"/>
  <path d="M14 36 Q14 14 50 14 Q86 14 86 36Z" fill="url(#chw)" stroke="#1a0f05" stroke-width="2" ${open ? 'transform="rotate(-25 14 36)"' : ''}/>
  <rect x="44" y="30" width="12" height="16" rx="2" fill="#ffcf4a" stroke="#6a4a00"/><rect x="14" y="44" width="72" height="4" fill="#c9a040"/><rect x="24" y="36" width="5" height="36" fill="#c9a040"/><rect x="71" y="36" width="5" height="36" fill="#c9a040"/>
</svg>`;
