// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Modern Select Component Initialization
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

// hack to fix jquery 3.6 focus security patch that bugs auto search in select-2
$(document).on('select2:open', (e) => {
    let searchField = document.querySelector('.select2-container--open .select2-search__field');
    if (searchField) {
        searchField.focus();
    }
});

function validateDate() {
    let dateFrom = document.getElementById("dateFrom").value;
    let dateTo = document.getElementById("dateTo").value;
    if (dateFrom > dateTo) {
        alert("Date From must be less than or equal to Date To");
        return false;
    }
    return true;
}

function formatNumber(number) {
    return number.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatNumberToFour(number) {
    return number.toLocaleString('en-US', { minimumFractionDigits: 4, maximumFractionDigits: 4 });
}

function parseNumber(formattedNum) {
    return parseFloat(formattedNum.replace(/,/g, '')) || 0;
}

// Dynamic date to in books
document.addEventListener('DOMContentLoaded', function () {
    var dateFromInput = document.getElementById('DateFrom');
    var dateToInput = document.getElementById('DateTo');

    // Add an event listener to DateFrom input
    dateFromInput?.addEventListener('change', function () {
        // Set DateTo input value to DateFrom input value
        dateToInput.value = dateFromInput.value;
    });
});

// Format the tin number
document.addEventListener('DOMContentLoaded', () => {
    const inputFields = document.querySelectorAll('.formattedTinNumberInput');

    inputFields.forEach(inputField => {
        inputField.addEventListener('input', (e) => {
            let value = e.target.value.replace(/-/g, ''); // Remove existing dashes
            let formattedValue = '';

            // Add dashes after every 3 digits, keeping the last 5 digits without dashes
            for (let i = 0; i < value.length; i++) {
                if (i === 3 || i === 6 || i === 9) {
                    formattedValue += '-';
                }
                formattedValue += value[i];
            }

            // If there are more than 12 characters, don't add a dash after the 10th character (i.e., for the last 5 digits)
            if (formattedValue.length > 12) {
                formattedValue = formattedValue.substring(0, 12) + formattedValue.substring(12).replace(/-/g, '');
            }

            e.target.value = formattedValue;
        });

        inputField.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace') {
                let value = e.target.value;
                // Remove the dash when backspace is pressed if it is at the end of a section of 3 digits
                if (value.endsWith('-')) {
                    e.target.value = value.slice(0, -1);
                }
            }
        });
    });
});

//navigation bar dropend implementation
document.addEventListener("DOMContentLoaded", function () {
    // Get all dropend elements
    const dropends = document.querySelectorAll(".dropend");

    // Track the currently open parent dropend
    let openParentDropend = null;

    dropends.forEach(function (dropend) {
        dropend.addEventListener("click", function (event) {
            // Stop event from bubbling up
            event.stopPropagation();

            const clickedMenu = this.querySelector(".dropdown-menu");

            // If clicking on a child menu inside an open parent, allow it
            if (openParentDropend && openParentDropend.contains(this)) {
                return;
            }

            // Close the currently open parent dropend if different
            if (openParentDropend && openParentDropend !== this) {
                const openMenu = openParentDropend.querySelector(".dropdown-menu");
                if (openMenu) {
                    openMenu.classList.remove("show");
                }
            }

            // Open the clicked dropend
            if (clickedMenu) {
                clickedMenu.classList.add("show");
                openParentDropend = this;
            }
        });
    });
});

$(document).ready(function () {
    $('#dataTable').DataTable({
        stateSave: true,
        processing: true
    });
});
