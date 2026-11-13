// Generates the original decorative SVGs used by site/. Run: node tools/gen-site-art.js
const fs = require('fs');
const path = require('path');
const out = path.join(__dirname, '..', 'site', 'assets');
const w = (n, s) => fs.writeFileSync(path.join(out, n), s.trim() + '\n');

const BLUE = ['#3F7CEF', '#17BBEE'];
const ORANGE = ['#FF8B4A', '#EE511F'];
const grad = (id, c, x1 = 0, y1 = 0, x2 = 1, y2 = 1) =>
  `<linearGradient id="${id}" x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}"><stop stop-color="${c[0]}"/><stop offset="1" stop-color="${c[1]}"/></linearGradient>`;

// ---- icon glyphs (48x48 box, stroke based) -----------------------------
const snowflake = (s) => {
  const arms = [0, 60, 120].map((a) => `<g transform="rotate(${a} 24 24)"><path d="M24 5V43M18.5 10.5L24 16l5.5-5.5M18.5 37.5L24 32l5.5 5.5"/></g>`).join('');
  return `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none">${arms}</g>`;
};
const thermo = (s) => `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"><path d="M20 28V10a4 4 0 018 0v18a8 8 0 11-8 0z"/><path d="M24 20v14"/><circle cx="24" cy="35" r="2.5" fill="${s}"/></g>`;
const fan = (s) => `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"><circle cx="24" cy="24" r="3.5"/><path d="M24 20.5C24 10 31 6 36 9.5 38 15 31 20 24 20.5z"/><path d="M27.5 24C38 24 42 31 38.5 36 33 38 28 31 27.5 24z"/><path d="M24 27.5C24 38 17 42 12 38.5 10 33 17 28 24 27.5z"/><path d="M20.5 24C10 24 6 17 9.5 12 15 10 20 17 20.5 24z"/></g>`;
const bolt = (s) => `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"><path d="M27 5L12 27h11l-2 16 15-22H25z"/></g>`;
const toggle = (s) => `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"><rect x="5" y="14" width="38" height="20" rx="10"/><circle cx="33" cy="24" r="5" fill="${s}"/></g>`;
const tray = (s) => `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"><rect x="5" y="9" width="38" height="26" rx="4"/><path d="M5 40h38"/><path d="M33 40v-5"/><path d="M12 28h8"/></g>`;
const terminal = (s) => `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"><rect x="5" y="8" width="38" height="32" rx="5"/><path d="M13 19l7 5-7 5M24 31h10"/></g>`;
const shield = (s) => `<g stroke="${s}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" fill="none"><path d="M24 5l15 6v11c0 10-6.5 17-15 21-8.5-4-15-11-15-21V11z"/><path d="M17 24l5 5 9-10"/></g>`;

// f1..f6 : feature card icons (blue gradient stroke)
const cardIcons = { f1: snowflake, f2: toggle, f3: tray, f4: fan, f5: terminal, f6: shield };
for (const [name, fn] of Object.entries(cardIcons)) {
  w(`${name}.svg`, `<svg width="48" height="48" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg"><defs>${grad('g', BLUE)}</defs>${fn('url(#g)')}</svg>`);
}

// small list icons
w('happen.svg', `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><defs>${grad('g', ORANGE)}</defs><g stroke="url(#g)" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3v11"/><circle cx="12" cy="18" r="3.2"/></g></svg>`);
w('measure.svg', `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><defs>${grad('g', BLUE)}</defs><g stroke="url(#g)" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3v18M4.2 7.5l15.6 9M4.2 16.5l15.6-9"/></g></svg>`);

// features column: three icon tiles joined by lines (64 x 476)
const tile = (y, id, c, glyph) =>
  `<rect x="0.75" y="${y + 0.75}" width="62.5" height="62.5" rx="16.25" fill="#1A1A21"/>` +
  `<rect x="0.75" y="${y + 0.75}" width="62.5" height="62.5" rx="16.25" stroke="url(#${id})" stroke-width="1.5"/>` +
  `<g transform="translate(14 ${y + 14}) scale(0.75)">${glyph(`url(#${id}i)`)}</g>`;
const featuresCol = (c) => {
  const lg = (id) => grad(id, c, 0, 0, 1, 1) + grad(id + 'i', c);
  const line = (y1, y2) => `<path d="M32 ${y1}V${y2}" stroke="#2E2E3A" stroke-width="2" stroke-dasharray="4 6" stroke-linecap="round"/>`;
  return `<svg width="64" height="476" viewBox="0 0 64 476" fill="none" xmlns="http://www.w3.org/2000/svg"><defs>${lg('a')}${lg('b')}${lg('c')}</defs>` +
    line(76, 194) + line(282, 400) +
    tile(0, 'a', c, thermo) + tile(206, 'b', c, fan) + tile(412, 'c', c, bolt) + `</svg>`;
};
w('features.svg', featuresCol(BLUE));
w('features-off.svg', featuresCol(ORANGE));

// section 2 background glow (1448 x 814)
const glow = (c) => `<svg width="1448" height="814" viewBox="0 0 1448 814" fill="none" xmlns="http://www.w3.org/2000/svg"><defs><filter id="b" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="110"/></filter>${grad('g', c, 0, 0, 1, 1)}</defs><ellipse cx="572" cy="449" rx="361" ry="267" fill="url(#g)" opacity="0.3" filter="url(#b)"/><ellipse cx="1180" cy="300" rx="180" ry="140" fill="url(#g)" opacity="0.12" filter="url(#b)"/></svg>`;
w('s2.svg', glow(BLUE));
w('s2-off.svg', glow(ORANGE));

// hero (928 x 720): glowing chip with snowflake, orbiting nodes and traces
let traces = '';
const pts = [[464, 360]];
const rays = [0, 45, 90, 135, 180, 225, 270, 315];
rays.forEach((a, i) => {
  const r = a * Math.PI / 180, x1 = 464 + Math.cos(r) * 150, y1 = 360 + Math.sin(r) * 150;
  const x2 = 464 + Math.cos(r) * (240 + (i % 3) * 40), y2 = 360 + Math.sin(r) * (240 + (i % 3) * 40);
  traces += `<path d="M${x1.toFixed(1)} ${y1.toFixed(1)}L${x2.toFixed(1)} ${y2.toFixed(1)}" stroke="#3F7CEF" stroke-opacity="0.45" stroke-width="2"/><circle cx="${x2.toFixed(1)}" cy="${y2.toFixed(1)}" r="5" fill="#17BBEE" fill-opacity="0.8"/>`;
});
w('s1.svg', `<svg width="928" height="720" viewBox="0 0 928 720" fill="none" xmlns="http://www.w3.org/2000/svg"><defs><filter id="b" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="90"/></filter>${grad('g', BLUE)}${grad('c', ['#1C2533', '#101014'], 0, 0, 1, 1)}</defs>
<ellipse cx="464" cy="360" rx="300" ry="260" fill="url(#g)" opacity="0.28" filter="url(#b)"/>
${traces}
<circle cx="464" cy="360" r="200" stroke="#3F7CEF" stroke-opacity="0.18" stroke-dasharray="3 9"/>
<circle cx="464" cy="360" r="290" stroke="#17BBEE" stroke-opacity="0.10" stroke-dasharray="3 12"/>
<rect x="354" y="250" width="220" height="220" rx="44" fill="url(#c)" stroke="url(#g)" stroke-width="3"/>
<g transform="translate(384 280) scale(3.125)">${snowflake('url(#g)')}</g>
</svg>`);

// section 6 backdrop: faint snowflake field (1342 x 614)
let flakes = '';
[[160, 480, 1.6], [420, 540, 1], [760, 500, 1.3], [1050, 540, 1.8], [1240, 420, 1], [60, 300, 0.9], [640, 580, 0.8]].forEach(([x, y, s]) => {
  flakes += `<g opacity="0.07" transform="translate(${x} ${y}) scale(${s * 2})">${snowflake('#17BBEE')}</g>`;
});
w('s6.svg', `<svg width="1342" height="614" viewBox="0 0 1342 614" fill="none" xmlns="http://www.w3.org/2000/svg">${flakes}</svg>`);
