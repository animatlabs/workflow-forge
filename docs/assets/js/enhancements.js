/**
 * WorkflowForge Documentation - UX Enhancements
 * - Back-to-top button (appears on scroll)
 * - External links open in new tab
 * - Smooth scrolling for anchor links
 */
(function () {
  'use strict';

  /* ===== Back-to-top button ===== */
  var btn = document.createElement('button');
  btn.id = 'back-to-top';
  btn.setAttribute('aria-label', 'Back to top');
  btn.title = 'Back to top';
  btn.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="18 15 12 9 6 15"/></svg>';
  document.body.appendChild(btn);

  // CSS injected inline so no separate file needed
  var style = document.createElement('style');
  style.textContent =
    '#back-to-top{position:fixed;bottom:2rem;right:2rem;z-index:1000;' +
    'width:40px;height:40px;border-radius:50%;border:none;cursor:pointer;' +
    'display:flex;align-items:center;justify-content:center;' +
    'background:var(--wf-primary);color:#fff;' +
    'box-shadow:var(--wf-shadow);opacity:0;visibility:hidden;' +
    'transition:opacity .3s ease,visibility .3s ease,transform .15s ease;}' +
    '#back-to-top.visible{opacity:1;visibility:visible;}' +
    '#back-to-top:hover{transform:translateY(-2px);background:var(--wf-primary-dark);}';
  document.head.appendChild(style);

  var scrollThreshold = 400;
  window.addEventListener('scroll', function () {
    btn.classList.toggle('visible', window.scrollY > scrollThreshold);
  }, { passive: true });

  btn.addEventListener('click', function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  });

  /* ===== External links open in new tab ===== */
  var links = document.querySelectorAll('a[href^="http"]');
  var host = window.location.hostname;
  links.forEach(function (link) {
    if (link.hostname !== host) {
      link.setAttribute('target', '_blank');
      link.setAttribute('rel', 'noopener noreferrer');
    }
  });

  /* ===== Smooth scrolling for anchor links ===== */
  document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
    anchor.addEventListener('click', function (e) {
      var id = this.getAttribute('href');
      if (id && id.length > 1) {
        var target = document.querySelector(id);
        if (target) {
          e.preventDefault();
          target.scrollIntoView({ behavior: 'smooth', block: 'start' });
          // Update URL without jumping
          history.pushState(null, null, id);
        }
      }
    });
  });
})();
