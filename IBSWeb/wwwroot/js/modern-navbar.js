/**
 * modern-navbar.js
 * Opt-in modern mega-menu navbar controller.
 *
 * Features:
 *  - localStorage opt-in toggle ("ibs_modern_nav" = "1")
 *  - Mega-menu open/close (click + Escape)
 *  - Notification badge sync from classic hidden counter
 *  - Spotlight search — scans all nav links, opens a results dropdown
 *  - Suppresses Quick Access sidebar when modern nav is active
 */

(function () {
    'use strict';

    const PREF_KEY   = 'ibs_modern_nav';
    const CLASSIC_ID = 'classic-navbar-header';
    const MODERN_ID  = 'modern-navbar';

    /* ══════════════════════════════════════════════════════
       Preference helpers
    ══════════════════════════════════════════════════════ */
    function isEnabled() { return localStorage.getItem(PREF_KEY) === '1'; }
    function setEnabled(val) { localStorage.setItem(PREF_KEY, val ? '1' : '0'); }

    /* ══════════════════════════════════════════════════════
       Quick Access sidebar — suppress / restore
    ══════════════════════════════════════════════════════ */
    function setQuickAccessVisible(visible) {
        // Panel
        const panel   = document.getElementById('qa-panel');
        const trigger = document.getElementById('qa-nav-trigger');

        if (panel) {
            if (!visible) {
                panel.dataset.mnavHidden = panel.classList.contains('qa-hidden') ? 'was-hidden' : 'was-visible';
                panel.classList.add('qa-hidden');
                panel.style.display = 'none';
            } else {
                panel.style.display = '';
                // Restore previous state
                if (panel.dataset.mnavHidden === 'was-visible') {
                    panel.classList.remove('qa-hidden');
                }
                delete panel.dataset.mnavHidden;
            }
        }

        // Nav trigger bolt icon in classic navbar
        if (trigger) {
            trigger.style.display = visible ? '' : 'none';
        }
    }

    /* ══════════════════════════════════════════════════════
       Apply / remove modern nav state
    ══════════════════════════════════════════════════════ */
    function applyState(enabled) {
        if (!document.getElementById(CLASSIC_ID) || !document.getElementById(MODERN_ID)) return;

        // The layout wrapper carries `style="padding-top:5rem !important"`.
        // Inline !important can't be overridden from a stylesheet — must use JS.
        const wrapper = document.querySelector('body > div[class*="container"]');

        document.body.classList.toggle('mnav-enabled', enabled);
        if (wrapper) wrapper.style.setProperty('padding-top', enabled ? '0' : '5rem', 'important');
        setQuickAccessVisible(!enabled);

        document.querySelectorAll('[data-mnav-toggle]').forEach(btn => {
            const icon  = btn.querySelector('.material-symbols-outlined');
            const label = btn.querySelector('.mnav-toggle-label');
            if (icon)  icon.textContent  = enabled ? 'toggle_on'  : 'toggle_off';
            if (label) label.textContent = enabled ? 'Modern UI' : 'Try Modern UI';
            btn.setAttribute('title', enabled ? 'Switch back to the classic user interface' : 'Try the new modern user interface');
        });
    }

    /* ══════════════════════════════════════════════════════
       Mega-menu open/close
    ══════════════════════════════════════════════════════ */
    function setupMegaMenus() {
        const overlay = document.getElementById('mnav-overlay');
        const items   = document.querySelectorAll('.mnav-item[data-mega]');

        function closeAll() {
            items.forEach(i => i.classList.remove('mnav-open'));
            if (overlay) overlay.classList.remove('active');
        }

        items.forEach(item => {
            const trigger = item.querySelector('.mnav-trigger');
            if (!trigger) return;
            trigger.addEventListener('click', e => {
                e.stopPropagation();
                const wasOpen = item.classList.contains('mnav-open');
                closeAll();
                if (!wasOpen) {
                    item.classList.add('mnav-open');
                    if (overlay) overlay.classList.add('active');
                }
            });
        });

        if (overlay) overlay.addEventListener('click', closeAll);
        document.addEventListener('keydown', e => { if (e.key === 'Escape') closeAll(); });
    }

    /* ══════════════════════════════════════════════════════
       Notification badge sync
    ══════════════════════════════════════════════════════ */
    function syncNotificationBadge() {
        const existing = document.getElementById('notificationCount');
        const modern   = document.getElementById('mnav-notif-count');
        if (!existing || !modern) return;

        function sync() {
            const n = existing.textContent.trim();
            modern.textContent = n;
            modern.style.display = (n === '0' || n === '') ? 'none' : '';
        }

        new MutationObserver(sync).observe(existing, { childList: true, characterData: true, subtree: true });
        sync();
    }

    /* ══════════════════════════════════════════════════════
       Spotlight search
    ══════════════════════════════════════════════════════ */

    /**
     * Collect every navigable link from BOTH the classic navbar (for
     * the quick-access sidebar compatibility) AND the modern navbar links.
     */
    function collectAllNavLinks() {
        const seen  = new Set();
        const links = [];

        function sanitize(url) {
            if (!url || url === '#' || url === '') return null;
            try {
                const p = new URL(url, window.location.origin);
                if (p.origin !== window.location.origin) return null;
                return p.pathname + p.search;
            } catch { return null; }
        }

        function getCleanLabel(anchor) {
            // Direct targeted selectors for known structures
            const titleEl = anchor.querySelector('.mnav-link-title, .font-title-md, .title-md');
            if (titleEl) {
                return titleEl.textContent.trim();
            }

            // Fallback: clone the node and strip out elements we don't want (like icons or sub-labels)
            const clone = anchor.cloneNode(true);
            
            // Remove icons and standard classes
            clone.querySelectorAll('.material-symbols-outlined, i, svg, .mnav-link-sub, .text-outline').forEach(el => el.remove());
            
            // Safely remove any elements with the brackets-containing Tailwind class
            clone.querySelectorAll('div, span, p').forEach(el => {
                if (el.classList.contains('text-[10px]')) {
                    el.remove();
                }
            });

            return clone.textContent.trim().replace(/\s+/g, ' ');
        }

        function push(anchor) {
            const raw   = anchor.getAttribute('href') || '';
            const url   = sanitize(raw);
            if (!url) return;
            // Skip home-ish links
            if (url === '/' || url.toLowerCase().includes('/home/')) return;
            const key = url;
            if (seen.has(key)) return;
            seen.add(key);

            const label = getCleanLabel(anchor);
            if (!label) return;

            // Try to get a section label from a parent heading or section label
            let section = '';
            let el = anchor.closest('.mnav-link-list, .mnav-mega');
            if (el) {
                const heading = el.querySelector('.mnav-section-label') ||
                                el.previousElementSibling;
                if (heading && heading.classList.contains('mnav-section-label')) {
                    section = heading.textContent.trim();
                }
            }

            links.push({ url, label, section });
        }

        // Modern navbar links
        document.querySelectorAll('#modern-navbar a[href]:not([href="#"]):not([data-search-ignore])').forEach(push);

        // Classic navbar links (still in DOM, just hidden)
        document.querySelectorAll(
            '#classic-navbar-header a.dropdown-item:not([href="#"]),' +
            '#classic-navbar-header a.nav-link:not([href="#"])'
        ).forEach(a => {
            if (!a.classList.contains('dropdown-toggle')) push(a);
        });

        return links;
    }

    function buildSearchDropdown() {
        const drop = document.createElement('div');
        drop.id = 'mnav-search-results';
        return drop;
    }

    function renderSearchResults(drop, query) {
        drop.innerHTML = '';

        if (!query) { drop.style.display = 'none'; return; }

        const q     = query.toLowerCase();
        const links = collectAllNavLinks().filter(l =>
            l.label.toLowerCase().includes(q) ||
            l.url.toLowerCase().includes(q)
        );

        if (links.length === 0) {
            drop.style.display = 'block';
            drop.innerHTML = `
                <div class="mnav-search-empty">
                    <span class="material-symbols-outlined">search_off</span>
                    No results for "<strong>${escapeHtml(query)}</strong>"
                </div>`;
            return;
        }

        drop.style.display = 'block';

        // Group by section
        const grouped = {};
        links.forEach(l => {
            const sec = l.section || 'Navigation';
            if (!grouped[sec]) grouped[sec] = [];
            grouped[sec].push(l);
        });

        Object.entries(grouped).forEach(([sec, items]) => {
            const header = document.createElement('div');
            header.className = 'mnav-search-section-header';
            header.textContent = sec;
            drop.appendChild(header);

            items.forEach(l => {
                const a = document.createElement('a');
                a.href = l.url;
                a.className = 'mnav-search-result-item';
                a.innerHTML = `
                    <span class="material-symbols-outlined">chevron_right</span>
                    <span>${highlightMatch(escapeHtml(l.label), escapeHtml(query))}</span>`;
                drop.appendChild(a);
            });
        });
    }

    function escapeHtml(str) {
        return str.replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    }

    function highlightMatch(label, query) {
        const idx = label.toLowerCase().indexOf(query.toLowerCase());
        if (idx === -1) return label;
        return label.slice(0, idx) +
               `<mark>${label.slice(idx, idx + query.length)}</mark>` +
               label.slice(idx + query.length);
    }

    function setupSearch() {
        const wrap  = document.querySelector('.mnav-search-wrap');
        const input = document.querySelector('.mnav-search');
        if (!wrap || !input) return;

        // Position wrap relatively so dropdown anchors to it
        wrap.style.position = 'relative';

        const drop = buildSearchDropdown();
        wrap.appendChild(drop);

        input.addEventListener('input', () => {
            renderSearchResults(drop, input.value.trim());
        });

        input.addEventListener('keydown', e => {
            if (e.key === 'Escape') {
                input.value = '';
                drop.style.display = 'none';
                input.blur();
            }
            if (e.key === 'Enter') {
                const first = drop.querySelector('a');
                if (first) first.click();
            }
            // Arrow key navigation
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                e.preventDefault();
                const items = [...drop.querySelectorAll('a')];
                if (!items.length) return;
                const focused = drop.querySelector('a:focus');
                const idx = focused ? items.indexOf(focused) : -1;
                const next = e.key === 'ArrowDown'
                    ? items[(idx + 1) % items.length]
                    : items[(idx - 1 + items.length) % items.length];
                next.focus();
            }
        });

        // Close on outside click
        document.addEventListener('click', e => {
            if (!wrap.contains(e.target)) {
                drop.style.display = 'none';
            }
        });

        // Keyboard shortcut: / focuses search (when modern nav active)
        document.addEventListener('keydown', e => {
            if (!document.body.classList.contains('mnav-enabled')) return;
            if (e.key === '/' && document.activeElement.tagName !== 'INPUT' && document.activeElement.tagName !== 'TEXTAREA') {
                e.preventDefault();
                input.focus();
                input.select();
            }
        });
    }

    /* ══════════════════════════════════════════════════════
        Mobile drawer
    ══════════════════════════════════════════════════════ */
    function setupMobileDrawer() {
        var hamburger = document.getElementById('mnav-hamburger');
        var drawer = document.getElementById('mnav-drawer');
        var overlay = document.getElementById('mnav-drawer-overlay');
        if (!hamburger || !drawer || !overlay) return;

        // Clone desktop nav links into the drawer
        var desktopList = document.querySelector('#modern-navbar .mnav-nav-area .mnav-list');
        var drawerList = drawer.querySelector('.mnav-list');
        if (desktopList && drawerList) {
            drawerList.innerHTML = '';
            desktopList.querySelectorAll('.mnav-item').forEach(function(item) {
                drawerList.appendChild(item.cloneNode(true));
            });
            // Re-bind mega-menu toggle on drawer items
            drawerList.querySelectorAll('.mnav-item[data-mega]').forEach(function(item) {
                var trigger = item.querySelector('.mnav-trigger');
                if (!trigger) return;
                trigger.addEventListener('click', function(e) {
                    e.stopPropagation();
                    var wasOpen = item.classList.contains('mnav-open');
                    drawerList.querySelectorAll('.mnav-item').forEach(function(i) { i.classList.remove('mnav-open'); });
                    if (!wasOpen) item.classList.add('mnav-open');
                });
            });
        }

        function open() { document.body.classList.add('mnav-drawer-open'); overlay.classList.add('active'); }
        function close() { document.body.classList.remove('mnav-drawer-open'); overlay.classList.remove('active'); }

        hamburger.addEventListener('click', function() {
            if (document.body.classList.contains('mnav-drawer-open')) { close(); } else { open(); }
        });
        overlay.addEventListener('click', close);
        // Close on Escape
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && document.body.classList.contains('mnav-drawer-open')) close();
        });
        // Close drawer on link click
        drawer.addEventListener('click', function(e) {
            var link = e.target.closest('a[href]');
            if (link) close();
        });
    }

    /* ══════════════════════════════════════════════════════
        Toggle buttons
    ══════════════════════════════════════════════════════ */
    function bindToggleButtons() {
        document.querySelectorAll('[data-mnav-toggle]').forEach(btn => {
            btn.addEventListener('click', () => {
                const next = !isEnabled();
                setEnabled(next);
                applyState(next);
            });
        });
    }

    /* ══════════════════════════════════════════════════════
        Init
    ══════════════════════════════════════════════════════ */
    document.addEventListener('DOMContentLoaded', () => {
        applyState(isEnabled());
        setupMegaMenus();
        bindToggleButtons();
        syncNotificationBadge();
        setupSearch();
        setupMobileDrawer();
    });
})();
