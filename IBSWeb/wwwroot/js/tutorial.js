/**
 * ============================================================================
 * IBS INTERACTIVE GUIDED TOUR ENGINE (tutorial.js)
 * ============================================================================
 * An interactive, step-by-step page wizard and guide engine.
 * Supports auto-detecting HTML required fields, live unblocked field interaction,
 * custom ModernSelect/Select2 dropdown integration, and smart popover positioning.
 *
 * ----------------------------------------------------------------------------
 * 1. HOW TO IMPLEMENT ON A PAGE (CSHTML)
 * ----------------------------------------------------------------------------
 * Step A: Mark target HTML elements with data-tour-step="N"
 * Example:
 *   <div id="CustomerContainer" data-tour-step="1">
 *       <select asp-for="CustomerId" class="js-modern-select">...</select>
 *   </div>
 *   <input asp-for="Date" type="date" class="modern-input" data-tour-step="2" />
 *
 * Step B: Define step configuration array in @section Scripts:
 * Example:
 *   @section Scripts {
 *       <script>
 *           window.IBS_TOUR_STEPS = [
 *               { step: 1, title: "Customer", text: "Select the customer for this job order." },
 *               { step: 2, title: "Order Date", text: "Defaults to today. Change if needed." },
 *               { step: 3, title: "Planned Start", text: "Optional start time.", required: false },
 *               { selector: "#confirmCreateBtn", title: "Submit", text: "Click to create job order." }
 *           ];
 *       </script>
 *   }
 *
 * Step C: Add data-page-header attribute to Page Heading (Auto-injects (?) Help Icon):
 * Example:
 *   <h1 class="modern-headline-lg" data-page-header>Create Job Order</h1>
 *
 * ----------------------------------------------------------------------------
 * 2. STEP CONFIGURATION OPTIONS (window.IBS_TOUR_STEPS)
 * ----------------------------------------------------------------------------
 * - step        : (Number|String) Matches element with data-tour-step="N".
 * - selector    : (String) Fallback CSS selector (e.g. '#myBtn' or '.my-class').
 * - title       : (String) Heading text for the popover box.
 * - text        : (String) Description text explaining what to do.
 * - pos         : (String) Preferred placement: 'auto' | 'top' | 'bottom'. (Default: 'auto')
 * - required    : (Boolean) True = next disabled until filled; False = allowed empty.
 *                 (Default: Auto-detected via HTML required / data-val-required attributes)
 * - interactive : (Boolean) Unblocks field interaction during tour. (Default: true)
 * - autoAdvance : (Boolean) Auto-advances tour when field value changes. (Default: true)
 * ============================================================================
 */
(function () {
    'use strict';
    
    var steps = [];
    var idx = 0;
    var overlay, spot, box;
    var activeCleanups = [];

    var CSS = ''
        + '.tour-overlay{position:fixed;inset:0;z-index:10000;pointer-events:none;display:none}'
        + '.tour-overlay.active{display:block}'
        + '.tour-backdrop{position:fixed;inset:0;background:rgba(0,0,0,0.4);pointer-events:auto;transition:opacity 0.2s}'
        + '.tour-spot{position:fixed;box-shadow:0 0 0 9999px rgba(0,0,0,0.45);border-radius:6px;pointer-events:none;z-index:10001;transition:all 0.15s ease-out;outline:2px solid var(--primary, #005cbb);outline-offset:2px;}'
        + '.tour-interactive-active{position:relative !important;z-index:10002 !important;pointer-events:auto !important;}'
        + '.select2-container--open, .modern-select-dropdown.show{z-index:10005 !important;pointer-events:auto !important;}'
        + '.tour-box{position:fixed;max-width:340px;width:calc(100vw - 32px);background:var(--surface, #ffffff);color:var(--on-surface, #1a1c1e);border:1px solid var(--outline-variant, #c4c6cf);border-radius:12px;padding:16px;box-shadow:0 10px 30px rgba(0,0,0,0.25);z-index:10006;pointer-events:auto;transition:top 0.12s ease-out, left 0.12s ease-out;font-family:inherit}'
        + '.tour-box .tour-header{display:flex;align-items:center;justify-content:space-between;margin-bottom:8px}'
        + '.tour-box .tour-title{font-weight:700;font-size:15px;color:var(--on-surface, #1a1c1e);margin:0}'
        + '.tour-box .tour-text{font-size:13px;line-height:1.5;color:var(--on-surface-variant, #44474e);margin-bottom:14px}'
        + '.tour-box .tour-nav{display:flex;align-items:center;gap:8px}'
        + '.tour-box .tour-count{margin-left:auto;color:var(--outline, #74777f);font-size:12px;font-weight:500}';

    function injectHeaderHelpButton() {
        // Target element explicitly marked with data-page-header
        var header = document.querySelector('[data-page-header]');
        if (!header || header.dataset.tourHelpInjected) return;

        // Ensure header aligns icon inline
        var computedDisplay = window.getComputedStyle(header).display;
        if (computedDisplay === 'block' || computedDisplay === 'inline') {
            header.style.display = 'inline-flex';
            header.style.alignItems = 'center';
            header.style.gap = '8px';
        }

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-icon tour-header-help-btn';
        btn.title = 'Start Guided Tour';
        btn.style.cssText = 'background:transparent; border:none; padding:0; cursor:pointer; color:var(--outline, #74777f); display:inline-flex; align-items:center; opacity:0.75; transition:opacity 0.2s; vertical-align:middle;';
        btn.innerHTML = '<span class="material-symbols-outlined" style="font-size:20px;">help</span>';
        
        btn.addEventListener('mouseenter', function() { btn.style.opacity = '1'; });
        btn.addEventListener('mouseleave', function() { btn.style.opacity = '0.75'; });
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            window.startTour();
        });

        header.appendChild(btn);
        header.dataset.tourHelpInjected = 'true';
    }

    function injectDOM() {
        if (overlay) return;

        var style = document.createElement('style');
        style.textContent = CSS;
        document.head.appendChild(style);

        overlay = document.createElement('div');
        overlay.className = 'tour-overlay';
        overlay.innerHTML = '<div class="tour-backdrop"></div>'
            + '<div class="tour-spot"></div>'
            + '<div class="tour-box">'
            + '<div class="tour-header"><h3 class="tour-title"></h3></div>'
            + '<div class="tour-text"></div>'
            + '<div class="tour-nav">'
            + '<button type="button" class="modern-btn-secondary btn-sm tour-prev" style="padding:4px 10px; font-size:12px">Previous</button>'
            + '<button type="button" class="modern-btn-primary btn-sm tour-next" style="padding:4px 12px; font-size:12px">Next</button>'
            + '<button type="button" class="btn btn-sm btn-link tour-close" style="font-size:12px; color:var(--outline, #74777f)">Close</button>'
            + '<span class="tour-count"></span>'
            + '</div></div>';
        document.body.appendChild(overlay);

        spot = overlay.querySelector('.tour-spot');
        box = overlay.querySelector('.tour-box');

        overlay.querySelector('.tour-prev').addEventListener('click', function () {
            if (idx > 0) { idx--; showStep(); }
        });
        overlay.querySelector('.tour-next').addEventListener('click', function () {
            idx++;
            if (idx >= steps.length) endTour();
            else showStep();
        });
        overlay.querySelector('.tour-close').addEventListener('click', endTour);

        window.addEventListener('resize', updateSpotAndBox);
        window.addEventListener('scroll', updateSpotAndBox, true);
    }

    function clearActiveInteractive() {
        activeCleanups.forEach(function (cleanup) { cleanup(); });
        activeCleanups = [];
        document.querySelectorAll('.tour-interactive-active').forEach(function (el) {
            el.classList.remove('tour-interactive-active');
        });
    }

    function resolveElement(step) {
        if (step.step != null) {
            var el = document.querySelector('[data-tour-step="' + step.step + '"]');
            if (el) return el;
        }
        if (step.selector) {
            return document.querySelector(step.selector);
        }
        return null;
    }

    function collectSteps() {
        var rawSteps = window.IBS_TOUR_STEPS || [];
        return rawSteps.map(function (s) {
            return {
                step: s.step,
                selector: s.selector,
                title: s.title || '',
                text: s.text || '',
                pos: s.pos || 'auto',
                required: s.required,                // undefined means auto-detect HTML required attribute
                interactive: s.interactive !== false, // default to true (allow interacting with highlighted field)
                autoAdvance: s.autoAdvance !== false  // auto-advance to next step upon user action
            };
        });
    }

    function updateSpotAndBox() {
        if (!overlay || !overlay.classList.contains('active') || !steps[idx]) return;

        var step = steps[idx];
        var el = resolveElement(step);
        if (!el || !el.getBoundingClientRect) return;

        var r = el.getBoundingClientRect();
        var padding = 6;

        spot.style.left = Math.max(0, r.left - padding) + 'px';
        spot.style.top = Math.max(0, r.top - padding) + 'px';
        spot.style.width = (r.width + padding * 2) + 'px';
        spot.style.height = (r.height + padding * 2) + 'px';

        // Calculate Box placement
        var boxWidth = box.offsetWidth || 320;
        var boxHeight = box.offsetHeight || 160;
        var viewportW = window.innerWidth;
        var viewportH = window.innerHeight;

        var top, left;

        // Preferred placement: placing above if space permits or if element is dropdown
        var isSelect = el.matches('select, .modern-select-container, .js-modern-select') || el.querySelector('.modern-select-container, select');
        
        if (step.pos === 'top' || isSelect || (step.pos === 'auto' && r.bottom + boxHeight + 15 > viewportH && r.top - boxHeight - 15 > 0)) {
            top = r.top - boxHeight - 12;
            if (top < 16) top = r.bottom + 12; // Fallback to bottom if top gets cut off
        } else {
            top = r.bottom + 12;
        }

        left = r.left + (r.width / 2) - (boxWidth / 2);

        // Clamp to screen bounds
        if (left < 16) left = 16;
        if (left + boxWidth > viewportW - 16) left = viewportW - boxWidth - 16;
        if (top < 16) top = 16;
        if (top + boxHeight > viewportH - 16) top = viewportH - boxHeight - 16;

        box.style.left = left + 'px';
        box.style.top = top + 'px';
    }

    function isStepValid(step, el) {
        if (!el) return true;

        var $valEl = window.jQuery ? window.jQuery(el).find('select, input, textarea').addBack('select, input, textarea') : el.querySelector('select, input, textarea');
        var domNode = ($valEl && $valEl.length) ? $valEl[0] : null;

        // Determine if field is required (explicit step config OR HTML required attribute/data-val-required)
        var isRequired = (typeof step.required === 'boolean') 
            ? step.required 
            : (domNode && (domNode.hasAttribute('required') || domNode.hasAttribute('data-val-required') || domNode.required));

        if (!isRequired) return true;

        if (domNode) {
            var val = window.jQuery ? $valEl.val() : domNode.value;
            return val != null && String(val).trim() !== '';
        }
        return true;
    }

    function updateNextButtonState(step, el) {
        var nextBtn = overlay.querySelector('.tour-next');
        if (!nextBtn) return;

        if (isStepValid(step, el)) {
            nextBtn.disabled = false;
            nextBtn.style.opacity = '1';
            nextBtn.style.cursor = 'pointer';
        } else {
            nextBtn.disabled = true;
            nextBtn.style.opacity = '0.5';
            nextBtn.style.cursor = 'not-allowed';
        }
    }

    function showStep() {
        clearActiveInteractive();

        var step = steps[idx];
        if (!step) { endTour(); return; }

        var el = resolveElement(step);
        if (!el) {
            // Poll briefly if dynamic element
            var tries = 0;
            var poll = setInterval(function () {
                tries++;
                var found = resolveElement(step);
                if (found) {
                    clearInterval(poll);
                    showStep();
                } else if (tries >= 10) {
                    clearInterval(poll);
                    console.warn('[tour] Step element missing:', step);
                    idx++;
                    if (idx >= steps.length) endTour();
                    else showStep();
                }
            }, 100);
            return;
        }

        // Scroll element into view smoothly
        el.scrollIntoView({ block: 'center', behavior: 'smooth' });

        // Enable interaction with highlighted element
        if (step.interactive) {
            el.classList.add('tour-interactive-active');
            
            // Also enable child select2/input if inside container
            var targetControls = el.querySelectorAll('input, select, textarea, button, .select2-container, .modern-select-container');
            targetControls.forEach(function (ctrl) {
                ctrl.classList.add('tour-interactive-active');
            });

            // Monitor input/change to re-evaluate Next button validity live
            var monitorHandler = function () {
                updateNextButtonState(step, el);
            };

            var $controls = window.jQuery ? window.jQuery(el).find('input, select, textarea').addBack('input, select, textarea') : null;
            if ($controls && $controls.length) {
                $controls.on('input.tourVal change.tourVal select2:select.tourVal', monitorHandler);
                activeCleanups.push(function () { $controls.off('input.tourVal change.tourVal select2:select.tourVal'); });
            }

            // If autoAdvance is set, listen for change/input to advance tour automatically once valid
            if (step.autoAdvance) {
                // Guard against input+change firing together (e.g. radio groups) causing a double advance
                var lastAdvanceAt = 0;
                var advanceHandler = function () {
                    var now = Date.now();
                    if (now - lastAdvanceAt < 250) return;
                    lastAdvanceAt = now;
                    setTimeout(function () {
                        if (overlay.classList.contains('active') && isStepValid(step, el)) {
                            idx++;
                            if (idx >= steps.length) endTour();
                            else showStep();
                        }
                    }, 50);
                };

                // Handle jQuery/ModernSelect change event if applicable
                var $targetSelect = window.jQuery ? window.jQuery(el).find('select').addBack('select') : null;
                if ($targetSelect && $targetSelect.length) {
                    $targetSelect.one('select2:select.tour change.tour', advanceHandler);
                    activeCleanups.push(function () { $targetSelect.off('select2:select.tour change.tour'); });
                } else {
                    // Advance on value change of contained inputs, not on raw click
                    var $containedInputs = window.jQuery ? window.jQuery(el).find('input, textarea') : null;
                    if ($containedInputs && $containedInputs.length) {
                        $containedInputs.one('change.tour input.tour', advanceHandler);
                        activeCleanups.push(function () { $containedInputs.off('change.tour input.tour'); });
                    } else {
                        var eventName = (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') ? 'change' : 'click';
                        el.addEventListener(eventName, advanceHandler, { once: true });
                        activeCleanups.push(function () { el.removeEventListener(eventName, advanceHandler); });
                    }
                }

                // If element has a ModernSelect container, flip popover above when dropdown opens
                var $trigger = window.jQuery ? window.jQuery(el).find('.modern-select-trigger') : null;
                if ($trigger && $trigger.length) {
                    var onOpen = function () {
                        setTimeout(updateSpotAndBox, 10);
                    };
                    $trigger.on('click.tour focus.tour', onOpen);
                    activeCleanups.push(function () { $trigger.off('click.tour focus.tour'); });
                }
            }
        }

        overlay.querySelector('.tour-title').textContent = step.title;
        overlay.querySelector('.tour-text').textContent = step.text;
        overlay.querySelector('.tour-prev').disabled = (idx === 0);
        overlay.querySelector('.tour-next').textContent = (idx === steps.length - 1) ? 'Finish' : 'Next';
        overlay.querySelector('.tour-count').textContent = (idx + 1) + ' / ' + steps.length;

        updateNextButtonState(step, el);

        // Give scroll time to settle before measuring coordinates
        setTimeout(updateSpotAndBox, 100);
    }

    function endTour() {
        clearActiveInteractive();
        if (overlay) overlay.classList.remove('active');
        idx = 0;
    }

    window.startTour = function () {
        injectDOM();
        steps = collectSteps();
        if (!steps.length) {
            if (typeof ModernAlert !== 'undefined' && ModernAlert.showToast) {
                ModernAlert.showToast('No tour steps defined for this page.', 'info');
            } else {
                alert('No tour steps defined for this page.');
            }
            return;
        }
        idx = 0;
        overlay.classList.add('active');
        showStep();
    };

    function init() {
        injectDOM();
        injectHeaderHelpButton();
        var navBtn = document.getElementById('mnav-tour-btn');
        if (navBtn) {
            navBtn.addEventListener('click', function (e) {
                e.preventDefault();
                var dd = document.getElementById('mnav-dropdown-menu');
                if (dd) dd.classList.remove('open');
                window.startTour();
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();



