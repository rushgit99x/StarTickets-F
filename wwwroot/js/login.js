document.addEventListener('DOMContentLoaded', function () {

    // Initialize login form enhancements
    initFormValidation();
    initFormSubmission();
    initInputAnimations();
    initAccessibility();
    initRememberMeCheckbox();

    /**
     * Initialize real-time form validation
     */
    function initFormValidation() {
        const form = document.querySelector('form[asp-action="Login"]');
        if (!form) return;

        const inputs = form.querySelectorAll('input[type="email"], input[type="password"]');

        inputs.forEach(input => {
            // Add validation on blur
            input.addEventListener('blur', function () {
                validateField(this);
            });

            // Add validation on input for immediate feedback
            input.addEventListener('input', function () {
                // Debounce validation to avoid excessive calls
                clearTimeout(this.validationTimeout);
                this.validationTimeout = setTimeout(() => {
                    validateField(this);
                }, 300);
            });
        });

        // Email specific validation
        const emailInput = document.querySelector('input[asp-for="Email"]');
        if (emailInput) {
            emailInput.addEventListener('input', function () {
                validateEmail(this);
            });
        }
    }

    /**
     * Validate individual form field
     */
    function validateField(field) {
        const value = field.value.trim();
        let isValid = true;
        let message = '';

        // Remove existing validation classes
        field.classList.remove('is-valid', 'is-invalid');

        // Required field validation
        if (field.hasAttribute('required') || field.getAttribute('data-val-required')) {
            if (!value) {
                isValid = false;
                message = 'This field is required';
            }
        }

        // Password minimum length validation
        if (field.type === 'password' && value && value.length < 6) {
            isValid = false;
            message = 'Password must be at least 6 characters';
        }

        // Apply validation styles
        if (value && isValid) {
            field.classList.add('is-valid');
            hideFieldError(field);
        } else if (!isValid) {
            field.classList.add('is-invalid');
            showFieldError(field, message);
        } else {
            hideFieldError(field);
        }
    }

    /**
     * Validate email format
     */
    function validateEmail(emailField) {
        const email = emailField.value.trim();
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        emailField.classList.remove('is-valid', 'is-invalid');

        if (email) {
            if (emailRegex.test(email)) {
                emailField.classList.add('is-valid');
                hideFieldError(emailField);
            } else {
                emailField.classList.add('is-invalid');
                showFieldError(emailField, 'Please enter a valid email address');
            }
        } else {
            hideFieldError(emailField);
        }
    }

    /**
     * Show field error message
     */
    function showFieldError(field, message) {
        let errorSpan = field.parentNode.querySelector('.validation-message');

        if (!errorSpan) {
            errorSpan = document.createElement('span');
            errorSpan.className = 'validation-message';
            field.parentNode.appendChild(errorSpan);
        }

        errorSpan.textContent = message;
        errorSpan.style.display = 'block';
    }

    /**
     * Hide field error message
     */
    function hideFieldError(field) {
        const errorSpan = field.parentNode.querySelector('.validation-message');
        if (errorSpan) {
            errorSpan.style.display = 'none';
        }
    }

    /**
     * Initialize form submission handling
     */
    function initFormSubmission() {
        const form = document.querySelector('form[asp-action="Login"]');
        const submitButton = document.querySelector('.login-btn');

        if (!form || !submitButton) return;

        form.addEventListener('submit', function (e) {
            // Add loading state to button
            submitButton.classList.add('loading');
            submitButton.disabled = true;

            // Validate all fields before submission
            const isValid = validateAllFields();

            if (!isValid) {
                e.preventDefault();
                submitButton.classList.remove('loading');
                submitButton.disabled = false;

                // Scroll to first error
                const firstError = document.querySelector('.is-invalid');
                if (firstError) {
                    firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    firstError.focus();
                }
                return;
            }

            // Save remember me preference before submission
            saveRememberMePreference();
        });

        // Remove loading state if form validation fails on server side
        window.addEventListener('load', function () {
            if (document.querySelector('.alert-danger') || document.querySelector('.text-danger')) {
                submitButton.classList.remove('loading');
                submitButton.disabled = false;
            }
        });
    }

    /**
     * Validate all form fields
     */
    function validateAllFields() {
        const form = document.querySelector('form[asp-action="Login"]');
        if (!form) return true;

        const inputs = form.querySelectorAll('input[type="email"], input[type="password"]');
        let isValid = true;

        inputs.forEach(input => {
            validateField(input);
            if (input.classList.contains('is-invalid')) {
                isValid = false;
            }
        });

        return isValid;
    }

    /**
     * Initialize input animations and interactions
     */
    function initInputAnimations() {
        const inputs = document.querySelectorAll('.modern-input');

        inputs.forEach(input => {
            // Add focus and blur animations
            input.addEventListener('focus', function () {
                this.parentNode.classList.add('focused');

                // Add subtle animation to the icon
                const icon = this.parentNode.querySelector('.input-icon');
                if (icon) {
                    icon.style.transform = 'translateY(-50%) scale(1.1)';
                    icon.style.color = 'var(--primary-color)';
                }
            });

            input.addEventListener('blur', function () {
                this.parentNode.classList.remove('focused');

                // Reset icon animation
                const icon = this.parentNode.querySelector('.input-icon');
                if (icon) {
                    icon.style.transform = 'translateY(-50%) scale(1)';
                }

                // Add filled class if input has value
                if (this.value.trim()) {
                    this.parentNode.classList.add('filled');
                } else {
                    this.parentNode.classList.remove('filled');
                }
            });

            // Check initial state
            if (input.value.trim()) {
                input.parentNode.classList.add('filled');
            }

            // Add typing effect
            input.addEventListener('input', function () {
                const icon = this.parentNode.querySelector('.input-icon');
                if (icon) {
                    icon.style.color = 'var(--primary-color)';
                }
            });
        });
    }

    /**
     * Initialize Remember Me checkbox functionality
     */
    function initRememberMeCheckbox() {
        const customCheckbox = document.querySelector('.custom-checkbox');
        const hiddenCheckbox = document.querySelector('input[asp-for="RememberMe"]');

        if (!customCheckbox || !hiddenCheckbox) return;

        // Load saved preference
        loadRememberMePreference();

        // Handle checkbox clicks
        function updateCheckboxState() {
            if (hiddenCheckbox.checked) {
                customCheckbox.classList.add('checked');
            } else {
                customCheckbox.classList.remove('checked');
            }
        }

        // Initial state
        updateCheckboxState();

        // Update state when checkbox changes
        hiddenCheckbox.addEventListener('change', updateCheckboxState);
    }

    /**
     * Initialize accessibility features
     */
    function initAccessibility() {
        // Add ARIA labels and descriptions
        const inputs = document.querySelectorAll('.modern-input');

        inputs.forEach(input => {
            const label = input.parentNode.querySelector('.modern-label');
            const errorSpan = input.parentNode.querySelector('.validation-message');

            if (label && !input.getAttribute('aria-label')) {
                input.setAttribute('aria-label', label.textContent.trim());
            }

            if (errorSpan) {
                const errorId = 'error-' + Math.random().toString(36).substr(2, 9);
                errorSpan.id = errorId;
                input.setAttribute('aria-describedby', errorId);
            }
        });

        // Add role and aria-live for alert messages
        const alerts = document.querySelectorAll('.alert-modern');
        alerts.forEach(alert => {
            alert.setAttribute('role', 'alert');
            alert.setAttribute('aria-live', 'polite');
        });

        // Improve keyboard navigation
        const focusableElements = document.querySelectorAll('.modern-input, .login-btn, .login-link, .custom-checkbox');
        focusableElements.forEach(element => {
            element.addEventListener('keydown', function (e) {
                if (e.key === 'Tab') {
                    element.classList.add('keyboard-focused');
                }
            });

            element.addEventListener('blur', function () {
                element.classList.remove('keyboard-focused');
            });
        });
    }

    /**
     * Save remember me preference to localStorage
     */
    function saveRememberMePreference() {
        const rememberMeCheckbox = document.querySelector('input[asp-for="RememberMe"]');
        if (rememberMeCheckbox) {
            try {
                localStorage.setItem('rememberMePreference', rememberMeCheckbox.checked.toString());
            } catch (e) {
                console.warn('Could not save remember me preference:', e);
            }
        }
    }

    /**
     * Load remember me preference from localStorage
     */
    function loadRememberMePreference() {
        try {
            const savedPreference = localStorage.getItem('rememberMePreference');
            if (savedPreference === 'true') {
                const rememberMeCheckbox = document.querySelector('input[asp-for="RememberMe"]');
                const customCheckbox = document.querySelector('.custom-checkbox');

                if (rememberMeCheckbox && customCheckbox) {
                    rememberMeCheckbox.checked = true;
                    customCheckbox.classList.add('checked');
                }
            }
        } catch (e) {
            console.warn('Could not load remember me preference:', e);
        }
    }

    /**
     * Auto-focus first empty field
     */
    function autoFocusFirstEmptyField() {
        const emailInput = document.querySelector('input[asp-for="Email"]');
        const passwordInput = document.querySelector('input[asp-for="Password"]');

        if (emailInput && !emailInput.value.trim()) {
            emailInput.focus();
        } else if (passwordInput && !passwordInput.value.trim()) {
            passwordInput.focus();
        }
    }

    // Auto-focus on page load (with slight delay to avoid issues)
    setTimeout(autoFocusFirstEmptyField, 100);

    /**
     * Handle Enter key to submit form
     */
    function initKeyboardSubmission() {
        const inputs = document.querySelectorAll('.modern-input');

        inputs.forEach(input => {
            input.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    const submitButton = document.querySelector('.login-btn');
                    if (submitButton && !submitButton.disabled) {
                        submitButton.click();
                    }
                }
            });
        });
    }

    initKeyboardSubmission();

    /**
     * Show/hide password toggle (if you want to add this feature)
     */
    function initPasswordToggle() {
        const passwordInput = document.querySelector('input[asp-for="Password"]');
        if (!passwordInput) return;

        // Create toggle button
        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'password-toggle';
        toggleButton.innerHTML = '<i class="fas fa-eye"></i>';
        toggleButton.style.cssText = `
            position: absolute;
            right: 1rem;
            top: 50%;
            transform: translateY(-50%);
            background: none;
            border: none;
            color: var(--primary-color);
            cursor: pointer;
            z-index: 3;
            margin-top: 0.75rem;
        `;

        // Insert toggle button
        passwordInput.parentNode.style.position = 'relative';
        passwordInput.parentNode.appendChild(toggleButton);

        // Handle toggle functionality
        toggleButton.addEventListener('click', function () {
            const icon = this.querySelector('i');

            if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
                icon.className = 'fas fa-eye-slash';
            } else {
                passwordInput.type = 'password';
                icon.className = 'fas fa-eye';
            }
        });
    }

    // Uncomment the line below if you want password toggle functionality
    // initPasswordToggle();

});

/**
 * Global function to toggle remember me checkbox (called from the view)
 */
function toggleRememberMe() {
    const hiddenCheckbox = document.querySelector('input[asp-for="RememberMe"]');
    const customCheckbox = document.querySelector('.custom-checkbox');

    if (hiddenCheckbox && customCheckbox) {
        hiddenCheckbox.checked = !hiddenCheckbox.checked;

        if (hiddenCheckbox.checked) {
            customCheckbox.classList.add('checked');
        } else {
            customCheckbox.classList.remove('checked');
        }

        // Trigger change event
        hiddenCheckbox.dispatchEvent(new Event('change'));
    }
}

/**
 * Manually trigger form validation (utility function)
 */
function validateLoginForm() {
    const form = document.querySelector('form[asp-action="Login"]');
    if (!form) return false;

    const inputs = form.querySelectorAll('input[type="email"], input[type="password"]');
    let isValid = true;

    inputs.forEach(input => {
        const event = new Event('blur', { bubbles: true });
        input.dispatchEvent(event);

        if (input.classList.contains('is-invalid')) {
            isValid = false;
        }
    });

    return isValid;
}

/**
 * Reset form validation states
 */
function resetLoginFormValidation() {
    const inputs = document.querySelectorAll('.modern-input');
    inputs.forEach(input => {
        input.classList.remove('is-valid', 'is-invalid');
        const errorSpan = input.parentNode.querySelector('.validation-message');
        if (errorSpan) {
            errorSpan.style.display = 'none';
        }
    });
}

/**
 * Focus on first error field
 */
function focusFirstLoginError() {
    const firstError = document.querySelector('.modern-input.is-invalid');
    if (firstError) {
        firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        firstError.focus();
        return true;
    }
    return false;
}

/**
 * Show success message
 */
function showLoginSuccessMessage(message) {
    const existingAlert = document.querySelector('.alert-success');
    if (existingAlert) {
        existingAlert.remove();
    }

    const alert = document.createElement('div');
    alert.className = 'alert alert-success alert-modern';
    alert.setAttribute('role', 'alert');
    alert.innerHTML = `<i class="fas fa-check-circle me-2"></i>${message}`;

    const form = document.querySelector('form[asp-action="Login"]');
    form.parentNode.insertBefore(alert, form);

    // Remove after 5 seconds
    setTimeout(() => {
        if (alert.parentNode) {
            alert.remove();
        }
    }, 5000);
}

/**
 * Show error message
 */
function showLoginErrorMessage(message) {
    const existingAlert = document.querySelector('.alert-danger');
    if (existingAlert) {
        existingAlert.remove();
    }

    const alert = document.createElement('div');
    alert.className = 'alert alert-danger alert-modern';
    alert.setAttribute('role', 'alert');
    alert.innerHTML = `<i class="fas fa-exclamation-circle me-2"></i>${message}`;

    const form = document.querySelector('form[asp-action="Login"]');
    form.parentNode.insertBefore(alert, form);

    // Remove after 5 seconds
    setTimeout(() => {
        if (alert.parentNode) {
            alert.remove();
        }
    }, 5000);
}

/**
 * Handle form auto-save for email (optional feature)
 */
function initEmailAutoSave() {
    const emailInput = document.querySelector('input[asp-for="Email"]');
    if (!emailInput) return;

    // Load saved email on page load
    try {
        const savedEmail = localStorage.getItem('loginEmail');
        if (savedEmail && savedEmail.includes('@')) {
            emailInput.value = savedEmail;
            emailInput.parentNode.classList.add('filled');
        }
    } catch (e) {
        console.warn('Could not load saved email:', e);
    }

    // Save email on input
    emailInput.addEventListener('input', debounce(function () {
        try {
            if (this.value.includes('@') && this.value.length > 5) {
                localStorage.setItem('loginEmail', this.value);
            }
        } catch (e) {
            console.warn('Could not save email:', e);
        }
    }, 1000));
}

/**
 * Utility function to debounce function calls
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func.apply(this, args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Handle page visibility change to pause/resume animations
 */
function initVisibilityHandling() {
    document.addEventListener('visibilitychange', function () {
        const floatingElements = document.querySelector('.login-container::after');
        if (document.hidden) {
            // Pause animations when page is not visible
            document.body.style.setProperty('--animation-play-state', 'paused');
        } else {
            // Resume animations when page becomes visible
            document.body.style.setProperty('--animation-play-state', 'running');
        }
    });
}

// Initialize optional features
// Uncomment these lines to enable additional features:

// Auto-save email feature
// setTimeout(initEmailAutoSave, 100);

// Visibility handling for performance
setTimeout(initVisibilityHandling, 100);

// Handle browser back/forward navigation
window.addEventListener('pageshow', function (event) {
    if (event.persisted) {
        // Reset form state if page was loaded from cache
        const submitButton = document.querySelector('.login-btn');
        if (submitButton) {
            submitButton.classList.remove('loading');
            submitButton.disabled = false;
        }
        resetLoginFormValidation();
    }
});

// Prevent form resubmission on page refresh
window.addEventListener('beforeunload', function () {
    const submitButton = document.querySelector('.login-btn');
    if (submitButton && submitButton.classList.contains('loading')) {
        submitButton.classList.remove('loading');
        submitButton.disabled = false;
    }
});