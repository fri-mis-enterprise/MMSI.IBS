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
        $trigger.on('click', function(e) {
            e.stopPropagation();
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
        
        $dropdown.on('click', '.modern-select-option', function(e) {
            e.stopPropagation();
            const val = $(this).data('value');
            const text = $(this).text();
            
            $select.val(val).trigger('change');
            $trigger.find('.selected-text').text(text);
            $dropdown.removeClass('show');
            $trigger.removeClass('active');
            
            $optionsContainer.find('.modern-select-option').removeClass('selected');
            $(this).addClass('selected');
        });
        
        if (isSearchable) {
            $dropdown.find('input').on('input', function() {
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
