/**
 * ModernAlert Utility
 * A custom UI wrapper for SweetAlert2 that replicates the user-provided "Success Notification" design.
 * Now using explicit CSS classes from modern-ui.css to avoid Tailwind dependencies.
 */

const ModernAlert = {
    // Helper to get the hero icon HTML
    _getHeroIcon: function(type) {
        const iconConfigs = {
            success: { icon: 'check_circle', color: 'icon-success', bg: 'icon-bg-success', pulse: 'pulse-success' },
            error: { icon: 'error', color: 'icon-error', bg: 'icon-bg-error', pulse: 'pulse-error' },
            warning: { icon: 'warning', color: 'icon-warning', bg: 'icon-bg-warning', pulse: 'pulse-warning' },
            info: { icon: 'info', color: 'icon-info', bg: 'icon-bg-info', pulse: 'pulse-info' },
            question: { icon: 'help', color: 'icon-question', bg: 'icon-bg-question', pulse: 'pulse-question' }
        };
        const config = iconConfigs[type] || iconConfigs.info;

        return `
            <div class="success-icon-container ${config.bg} mx-auto">
                <div class="absolute inset-0 rounded-full ${config.pulse} animate-ping opacity-75"></div>
                <span class="material-symbols-outlined ${config.color} icon-size-hero" style="font-variation-settings: 'FILL' 1;">${config.icon}</span>
            </div>
        `;
    },

    // Base configuration for the custom design
    _baseConfig: {
        customClass: {
            popup: 'modern-swal-popup',
            title: 'modern-swal-title',
            htmlContainer: 'modern-swal-html',
            confirmButton: 'modern-swal-confirm',
            cancelButton: 'modern-swal-cancel',
            actions: 'modern-swal-actions'
        },
        buttonsStyling: false,
        showCloseButton: false,
        allowOutsideClick: true,
        width: '384px',
        padding: '0',
        showClass: {
            popup: 'animate__animated animate__fadeInUp animate__faster'
        },
        hideClass: {
            popup: 'animate__animated animate__fadeOutDown animate__faster'
        }
    },

    /**
     * Show a success alert
     */
    success: function(message, title = 'Success') {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                ${this._getHeroIcon('success')}
                <p class="modern-value-md text-center max-w-[240px] mx-auto leading-relaxed mb-4" style="color: var(--on-surface-variant)">${message}</p>
                <div class="mt-6 flex items-center justify-center gap-2 opacity-40">
                    <div class="h-[1px] w-8" style="background-color: var(--outline-variant)"></div>
                    <span class="modern-label" style="font-size: 10px; margin-bottom: 0">System Verified</span>
                    <div class="h-[1px] w-8" style="background-color: var(--outline-variant)"></div>
                </div>
            `,
            confirmButtonText: 'OK',
            timer: 2500,
            timerProgressBar: true
        });
    },

    /**
     * Show an error alert
     */
    error: function(message, title = 'Error') {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                ${this._getHeroIcon('error')}
                <p class="modern-value-md text-center max-w-[240px] mx-auto leading-relaxed mb-4" style="color: var(--on-surface-variant)">${message}</p>
            `,
            confirmButtonText: 'OK'
        });
    },

    /**
     * Show a warning alert
     */
    warning: function(message, title = 'Warning') {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                ${this._getHeroIcon('warning')}
                <p class="modern-value-md text-center max-w-[240px] mx-auto leading-relaxed mb-4" style="color: var(--on-surface-variant)">${message}</p>
            `,
            confirmButtonText: 'OK'
        });
    },

    /**
     * Show an info alert
     */
    info: function(message, title = 'Info') {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                ${this._getHeroIcon('info')}
                <p class="modern-value-md text-center max-w-[240px] mx-auto leading-relaxed mb-4" style="color: var(--on-surface-variant)">${message}</p>
            `,
            confirmButtonText: 'OK',
            timer: 2000
        });
    },

    /**
     * Show a confirmation dialog
     */
    confirm: function({
        title = 'Confirmation',
        text = "Are you sure you want to proceed?",
        confirmText = 'OK',
        cancelText = 'Cancel',
        icon = 'question'
    } = {}) {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                ${this._getHeroIcon(icon)}
                <div class="modern-value-md text-center max-w-[240px] mx-auto leading-relaxed mb-4" style="color: var(--on-surface-variant)">${text}</div>
            `,
            showCancelButton: true,
            confirmButtonText: confirmText,
            cancelButtonText: cancelText
        });
    },

    /**
     * Show a simple alert
     */
    alert: function(title, text, icon = 'info') {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                ${this._getHeroIcon(icon)}
                <p class="modern-value-md text-center max-w-[240px] mx-auto leading-relaxed mb-4" style="color: var(--on-surface-variant)">${text}</p>
            `,
            confirmButtonText: 'OK'
        });
    },

    /**
     * Show a loading state
     */
    showLoading: function(title = 'Processing...') {
        Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                <div class="flex flex-col items-center py-4">
                    <div class="modern-loader-inline mb-4"></div>
                    <p class="modern-label">Please wait while we process your request.</p>
                </div>
            `,
            allowOutsideClick: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    },

    /**
     * Show an image preview
     */
    image: function(imageUrl, title = 'Image Preview') {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            imageUrl: imageUrl,
            imageAlt: title,
            showConfirmButton: false,
            background: 'transparent',
            width: 'auto',
            padding: '20px',
            didOpen: () => {
                const img = document.querySelector('.swal2-image');
                if (img) {
                    img.style.maxHeight = '80vh';
                    img.style.height = 'auto';
                    img.style.borderRadius = 'var(--radius-xl)';
                    img.style.boxShadow = 'var(--shadow-lg)';
                }
            }
        });
    },

    /**
     * Show a video preview
     */
    video: function(videoUrl, title = 'Video Preview') {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                <div class="p-2">
                    <video width="100%" height="auto" controls autoplay class="rounded-xl shadow-lg">
                        <source src="${videoUrl}" type="video/mp4">
                        Your browser does not support the video tag.
                    </video>
                </div>
            `,
            showConfirmButton: false,
            background: 'transparent',
            width: '80%',
            padding: '20px'
        });
    },

    /**
     * Show a prompt (input) dialog
     */
    prompt: function({
        title = 'Input Required',
        text = 'Please enter a value:',
        placeholder = '',
        confirmText = 'Submit',
        cancelText = 'Cancel',
        icon = 'question',
        validator = (value) => value ? null : 'Input is required'
    } = {}) {
        return Swal.fire({
            ...this._baseConfig,
            title: title,
            html: `
                ${this._getHeroIcon(icon)}
                <div class="modern-value-md text-center max-w-[240px] mx-auto leading-relaxed mb-4" style="color: var(--on-surface-variant)">${text}</div>
            `,
            input: 'text',
            inputPlaceholder: placeholder,
            inputAttributes: {
                class: 'form-control mx-auto w-full mb-4',
                style: 'max-width: 300px; border: 1px solid var(--outline-variant); border-radius: var(--radius-xl); padding: 12px 16px;'
            },
            showCancelButton: true,
            confirmButtonText: confirmText,
            cancelButtonText: cancelText,
            inputValidator: validator
        });
    },

    /**
     * Hide current SweetAlert
     */
    close: function() {
        Swal.close();
    }
};

/**
 * Global Compatibility Layer
 */
window.toast = {
    success: (msg) => ModernAlert.success(msg),
    error: (msg) => ModernAlert.error(msg),
    warning: (msg) => ModernAlert.warning(msg),
    info: (msg) => ModernAlert.info(msg)
};
window.toastr = window.toast;
window.swal = (title, text, icon) => (typeof title === 'object') ? Swal.fire(title) : ModernAlert.alert(title, text, icon);
window.swalConfirm = (title, text, callback) => {
    ModernAlert.confirm({ title, text }).then(result => {
        if (result.isConfirmed && typeof callback === 'function') callback();
    });
};
window.modernAlert = ModernAlert;
