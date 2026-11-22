// Launch-film intro: plays muted on load (browsers only autoplay muted video); the visitor can turn sound on,
// skip it (button or Esc), and replay it from the hero. If autoplay is blocked, the page shows right away.
(function () {
  var root = document.documentElement;
  var intro = document.getElementById('intro');
  var video = document.getElementById('intro-video');
  var sound = document.getElementById('intro-sound');
  var skip = document.getElementById('intro-skip');
  var bar = document.getElementById('intro-progress');
  var replay = document.getElementById('replay-film');
  var lastFocus = null;
  var src = window.matchMedia('(max-width: 900px)').matches ? 'assets/launch-720.mp4' : 'assets/launch.mp4';

  function setSound(on) {
    video.muted = !on;
    sound.setAttribute('aria-pressed', on ? 'true' : 'false');
    sound.setAttribute('aria-label', on ? 'Turn sound off' : 'Turn sound on');
  }
  function close() {
    if (!root.classList.contains('intro-on')) return;
    intro.classList.add('leaving');
    video.pause();
    setTimeout(function () {
      root.classList.remove('intro-on');
      intro.classList.remove('leaving');
      if (lastFocus) lastFocus.focus();
    }, 700);
  }
  function open(withSound) {
    if (!video.getAttribute('src')) video.setAttribute('src', src);
    lastFocus = document.activeElement;
    intro.classList.remove('leaving');
    root.classList.add('intro-on');
    video.currentTime = 0;
    setSound(!!withSound);
    var p = video.play();
    if (p && p.catch) p.catch(function () {
      // Autoplay refused (e.g. power saving): unmuted retry is not allowed, so don't block the page.
      if (!withSound) { root.classList.remove('intro-on'); }
    });
    skip.focus({ preventScroll: true });
  }

  sound.addEventListener('click', function () {
    setSound(video.muted);
    if (video.paused) video.play();
  });
  skip.addEventListener('click', close);
  video.addEventListener('ended', close);
  video.addEventListener('timeupdate', function () {
    if (video.duration) bar.style.width = (100 * video.currentTime / video.duration) + '%';
  });
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && root.classList.contains('intro-on')) close();
  });
  replay.addEventListener('click', function () { open(false); });

  if (root.classList.contains('intro-on')) open(false);
})();

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
