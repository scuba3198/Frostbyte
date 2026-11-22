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
  // Progress bar doubles as a seek bar: click or drag (mouse/touch), or arrow keys / Home / End when focused.
  var seek = document.getElementById('intro-seek');
  var dragging = false;
  function showTime(t) {
    if (!video.duration) return;
    bar.style.width = (100 * t / video.duration) + '%';
    seek.setAttribute('aria-valuemax', String(Math.round(video.duration)));
    seek.setAttribute('aria-valuenow', String(Math.round(t)));
    seek.setAttribute('aria-valuetext', Math.round(t) + ' seconds');
  }
  function seekTo(t) {
    if (!video.duration) return;
    t = Math.max(0, Math.min(video.duration - 0.05, t));
    video.currentTime = t;
    showTime(t);
  }
  function seekToPointer(e) {
    var r = seek.getBoundingClientRect();
    seekTo(video.duration * (e.clientX - r.left) / r.width);
  }
  video.addEventListener('timeupdate', function () { if (!dragging) showTime(video.currentTime); });
  seek.addEventListener('pointerdown', function (e) {
    dragging = true;
    seek.classList.add('dragging');
    seek.setPointerCapture(e.pointerId);
    seekToPointer(e);
  });
  seek.addEventListener('pointermove', function (e) { if (dragging) seekToPointer(e); });
  function endDrag() {
    if (!dragging) return;
    dragging = false;
    seek.classList.remove('dragging');
    if (video.paused && !video.ended) video.play();
  }
  seek.addEventListener('pointerup', endDrag);
  seek.addEventListener('pointercancel', endDrag);
  seek.addEventListener('keydown', function (e) {
    var step = { ArrowLeft: -2, ArrowDown: -2, ArrowRight: 2, ArrowUp: 2 }[e.key];
    if (step) { e.preventDefault(); seekTo(video.currentTime + step); }
    else if (e.key === 'Home') { e.preventDefault(); seekTo(0); }
    else if (e.key === 'End') { e.preventDefault(); seekTo(video.duration); }
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
