/**
 * ModernSelect Component
 * High-performance, searchable replacement for standard HTML selects.
 * Features: Fixed positioning, auto-flipping, and mutation observers.
 */
const ModernSelect = {
    init: function(selector) {
        $(selector).each(function() {
            ModernSelect.create($(this));
        });
    },

    create: function($select) {
        if ($select.data('modern-select-initialized')) return;
        
        const placeholder = $select.data('placeholder') || 'Select an option...';
        const isSearchable = $select.data('searchable') !== false;
        
        // Hide original select
        $select.hide();
        
        // Create modern select structure
        const $container = $('<div class="modern-select-container"></div>');
        const $trigger = $(`
            <div class="modern-select-trigger" tabindex="0">
                <span class="selected-text">${placeholder}</span>
                <span class="material-symbols-outlined">expand_more</span>
            </div>
        `);
        
        const $dropdown = $('<div class="modern-select-dropdown"></div>');
        
        // Append dropdown to body to avoid container clipping
        $('body').append($dropdown);
        
        if (isSearchable) {
            const $search = $(`
                <div class="modern-select-search">
                    <span class="material-symbols-outlined">search</span>
                    <input type="text" placeholder="Search...">
                </div>
            `);
            $dropdown.append($search);
        }
        
        const $optionsContainer = $('<div class="modern-select-options"></div>');
        const $noResults = $('<div class="modern-select-no-results">No results found</div>');
        
        $dropdown.append($optionsContainer).append($noResults);
        $container.append($trigger);
        $select.after($container);
        
        // Initial population
        ModernSelect.populate($select, $optionsContainer, $trigger, placeholder);
        
        // Helper to position dropdown
        const positionDropdown = () => {
            const rect = $trigger[0].getBoundingClientRect();
            $dropdown.css({
                'position': 'fixed',
                'top': (rect.bottom + 4) + 'px',
                'left': rect.left + 'px',
                'width': rect.width + 'px',
                'z-index': '9999'
            });

            // Flip up if hitting bottom of viewport
            const menuHeight = $dropdown.outerHeight();
            if (rect.bottom + menuHeight > window.innerHeight) {
                $dropdown.css('top', (rect.top - menuHeight - 4) + 'px');
            }
        };

        // Event Listeners
        let focusTime = 0;

        $trigger.on('focus', function() {
            if (!$dropdown.hasClass('show')) {
                focusTime = Date.now();
                $('.modern-select-dropdown').not($dropdown).removeClass('show');
                $('.modern-select-trigger').not($trigger).removeClass('active');
                $trigger.addClass('active');
                $dropdown.addClass('show');
                positionDropdown();
                
                if (isSearchable) {
                    setTimeout(() => $dropdown.find('input').val('').trigger('input').focus(), 10);
                }
            }
        });

        $trigger.on('click', function(e) {
            e.preventDefault();
            e.stopPropagation();

            // If focus event just fired (e.g. within 300ms), don't immediately toggle closed
            if (Date.now() - focusTime < 300) {
                return;
            }

            const isOpen = $dropdown.hasClass('show');
            
            $('.modern-select-dropdown').not($dropdown).removeClass('show');
            $('.modern-select-trigger').not($trigger).removeClass('active');
            
            if (!isOpen) {
                $trigger.addClass('active');
                $dropdown.addClass('show');
                positionDropdown();
                
                if (isSearchable) {
                    setTimeout(() => $dropdown.find('input').val('').trigger('input').focus(), 10);
                }
            } else {
                $trigger.removeClass('active');
                $dropdown.removeClass('show');
            }
        });
        
        const selectOption = ($opt) => {
            const val = $opt.data('value');
            const text = $opt.text();
            
            $select.val(val).trigger('change');
            $trigger.find('.selected-text').text(text);
            $dropdown.removeClass('show');
            $trigger.removeClass('active');
            
            $optionsContainer.find('.modern-select-option').removeClass('selected');
            $opt.addClass('selected');
        };

        $dropdown.on('click', '.modern-select-option', function(e) {
            e.preventDefault();
            e.stopPropagation();
            selectOption($(this));
        });
        
        if (isSearchable) {
            const $input = $dropdown.find('input');

            $input.on('input', function() {
                const term = $(this).val().toLowerCase();
                let hasResults = false;
                
                $optionsContainer.find('.modern-select-option').each(function() {
                    const text = $(this).text().toLowerCase();
                    if (text.includes(term)) {
                        $(this).removeClass('hidden');
                        hasResults = true;
                    } else {
                        $(this).addClass('hidden');
                    }
                });
                
                $noResults.toggle(!hasResults);

                // Clear any manual keyboard highlight when user types
                $optionsContainer.find('.modern-select-option.highlighted').removeClass('highlighted');

                // Auto-select if there is exactly 1 matching option and term is at least 2 characters long
                if (term.length >= 2) {
                    const $visibleOptions = $optionsContainer.find('.modern-select-option:not(.hidden)');
                    if ($visibleOptions.length === 1) {
                        selectOption($visibleOptions.first());
                    }
                }
            });

            // Keyboard navigation (Tab to highlight, Space/Enter to select)
            $input.on('keydown', function(e) {
                const $visibleOptions = $optionsContainer.find('.modern-select-option:not(.hidden)');
                
                if (e.key === 'Tab') {
                    if ($visibleOptions.length > 0) {
                        e.preventDefault(); // Stop focus from leaving the search box
                        
                        const $currentHighlighted = $optionsContainer.find('.modern-select-option.highlighted');
                        let nextIndex = 0;
                        
                        if ($currentHighlighted.length) {
                            const currentIndex = $visibleOptions.index($currentHighlighted);
                            if (e.shiftKey) {
                                // Cycle backwards
                                nextIndex = (currentIndex - 1 + $visibleOptions.length) % $visibleOptions.length;
                            } else {
                                // Cycle forwards
                                nextIndex = (currentIndex + 1) % $visibleOptions.length;
                            }
                            $currentHighlighted.removeClass('highlighted');
                        } else {
                            // If nothing is highlighted yet, Tab starts at 0, Shift+Tab starts at the end
                            nextIndex = e.shiftKey ? $visibleOptions.length - 1 : 0;
                        }
                        
                        const $nextOpt = $visibleOptions.eq(nextIndex);
                        $nextOpt.addClass('highlighted');
                        
                        // Scroll option into view if needed
                        const container = $optionsContainer[0];
                        const opt = $nextOpt[0];
                        if (opt.offsetTop < container.scrollTop) {
                            container.scrollTop = opt.offsetTop;
                        } else if (opt.offsetTop + opt.offsetHeight > container.scrollTop + container.clientHeight) {
                            container.scrollTop = opt.offsetTop + opt.offsetHeight - container.clientHeight;
                        }
                    }
                } else if (e.key === ' ' || e.key === 'Enter') {
                    const $highlighted = $optionsContainer.find('.modern-select-option.highlighted');
                    if ($highlighted.length) {
                        e.preventDefault(); // Prevent space character or form submit
                        selectOption($highlighted);
                    } else if (e.key === 'Enter' && $visibleOptions.length === 1) {
                        // Enter also selects the single option if not explicitly highlighted
                        e.preventDefault();
                        selectOption($visibleOptions.first());
                    }
                }
            });

            // Clear highlighted class when mouse moves to prevent dual highlight
            $optionsContainer.on('mouseenter', '.modern-select-option', function() {
                $optionsContainer.find('.modern-select-option.highlighted').removeClass('highlighted');
            });
        }
        
        // Sync back if select changes externally (e.g., cascading)
        $select.on('change', function() {
            const val = $select.val();
            const $selectedOption = $optionsContainer.find(`.modern-select-option[data-value="${val}"]`);
            
            if ($selectedOption.length) {
                $trigger.find('.selected-text').text($selectedOption.text());
                $optionsContainer.find('.modern-select-option').removeClass('selected');
                $selectedOption.addClass('selected');
            } else if (!val) {
                $trigger.find('.selected-text').text(placeholder);
                $optionsContainer.find('.modern-select-option').removeClass('selected');
            }
            // sync validation state to trigger element
            $trigger.toggleClass('is-invalid', $select.prop('required') && !val);
        });

        // Close on scroll to avoid disconnected dropdown
        $(window).on('scroll.modernSelect resize.modernSelect', function() {
            $('.modern-select-dropdown').removeClass('show');
            $('.modern-select-trigger').removeClass('active');
        });

        // Watch for mutations (if options are added dynamically)
        const observer = new MutationObserver(() => {
            ModernSelect.populate($select, $optionsContainer, $trigger, placeholder);
        });
        observer.observe($select[0], { childList: true });
        
        $select.data('modern-select-initialized', true);
    },

    populate: function($select, $optionsContainer, $trigger, placeholder) {
        $optionsContainer.empty();
        const currentVal = $select.val();
        let foundSelected = false;

        $select.find('option').each(function() {
            const val = $(this).val();
            const text = $(this).text();
            if (!val && !text.includes('--')) return; // Skip empty placeholder options if they aren't explicit
            
            const isSelected = val == currentVal;
            const $opt = $(`<div class="modern-select-option ${isSelected ? 'selected' : ''}" data-value="${val}">${text}</div>`);
            $optionsContainer.append($opt);
            
            if (isSelected) {
                $trigger.find('.selected-text').text(text);
                foundSelected = true;
            }
        });

        if (!foundSelected) {
            $trigger.find('.selected-text').text(placeholder);
        }
    }
};

$(document).on('click', function() {
    $('.modern-select-dropdown').removeClass('show');
    $('.modern-select-trigger').removeClass('active');
});

$(document).ready(function () {
    ModernSelect.init('.js-modern-select');
});
