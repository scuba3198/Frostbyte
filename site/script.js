document.getElementById('app-switcher').addEventListener('click', function () {
  document.getElementById('app-switcher-container').classList.toggle('off');
});
document.querySelectorAll('.happen>h4,.measure>h4').forEach(function (h) {
  h.addEventListener('click', function () { h.parentNode.classList.toggle('opened'); });
});
