document.getElementById('app-switcher').addEventListener('click', function () {
  document.getElementById('app-switcher-container').classList.toggle('off');
});
document.querySelectorAll('.happen>h4,.measure>h4').forEach(function (h) {
  h.addEventListener('click', function () { h.parentNode.classList.toggle('opened'); });
});

// Fill in the current version and installer size from the latest GitHub release.
// The values in the HTML are fallbacks if the request fails or is rate-limited.
fetch('https://api.github.com/repos/scuba3198/Frostbyte/releases/latest', { headers: { Accept: 'application/vnd.github+json' } })
  .then(function (r) { return r.ok ? r.json() : Promise.reject(r.status); })
  .then(function (rel) {
    var asset = (rel.assets || []).filter(function (a) { return /^Frostbyte-Setup-.*\.exe$/.test(a.name); })[0];
    var version = String(rel.tag_name || '').replace(/^v/, '');
    document.querySelectorAll('[data-release-version]').forEach(function (el) { if (version) el.textContent = version; });
    if (asset) {
      var size = (asset.size / 1048576).toFixed(1) + 'MB';
      document.querySelectorAll('[data-release-size]').forEach(function (el) { el.textContent = size; });
    }
  })
  .catch(function () {});
