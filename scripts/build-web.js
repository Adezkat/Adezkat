// Copies the game files into www/, which Capacitor bundles into the Android app.
const fs = require('fs'), path = require('path');
const root = path.join(__dirname, '..'), out = path.join(root, 'www');
fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(out);
for (const f of ['index.html', 'manifest.webmanifest', 'sw.js', 'css', 'js', 'icons']) {
  fs.cpSync(path.join(root, f), path.join(out, f), { recursive: true });
}
console.log('Web assets copied to www/');
