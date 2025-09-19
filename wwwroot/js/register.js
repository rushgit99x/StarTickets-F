document.addEventListener('DOMContentLoaded', function () {

    // Initialize form enhancements
    initFormValidation();
    initPasswordStrength();
    initFormSubmission();
    initInputAnimations();
    initAccessibility();

    /**
     * Initialize real-time form validation
     */
    function initFormValidation() {
        const form = document.querySelector('form[asp-action="Register"]');
        if (!form) return;

        const inputs = form.querySelectorAll('input, select');

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

        // Email validation
        const emailInput = document.querySelector('input[asp-for="Email"]');
        if (emailInput) {
            emailInput.addEventListener('input', function () {
                validateEmail(this);
            });
        }

        // Password confirmation validation
        const passwordInput = document.querySelector('input[asp-for="Password"]');
        const confirmPasswordInput = document.querySelector('input[asp-for="ConfirmPassword"]');

        if (passwordInput && confirmPasswordInput) {
            confirmPasswordInput.addEventListener('input', function () {
                validatePasswordMatch(passwordInput, confirmPasswordInput);
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

        // Apply validation styles
        if (value && isValid) {
            field.classList.add('is-valid');
        } else if (!isValid) {
            field.classList.add('is-invalid');
            showFieldError(field, message);
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
            } else {
                emailField.classList.add('is-invalid');
                showFieldError(emailField, 'Please enter a valid email address');
            }
        }
    }

    /**
     * Validate password match
     */
    function validatePasswordMatch(passwordField, confirmPasswordField) {
        const password = passwordField.value;
        const confirmPassword = confirmPasswordField.value;

        confirmPasswordField.classList.remove('is-valid', 'is-invalid');

        if (confirmPassword) {
            if (password === confirmPassword) {
                confirmPasswordField.classList.add('is-valid');
            } else {
                confirmPasswordField.classList.add('is-invalid');
                showFieldError(confirmPasswordField, 'Passwords do not match');
            }
        }
    }

    /**
     * Show field error message
     */
    function showFieldError(field, message) {
        let errorSpan = field.parentNode.querySelector('.field-validation-error');
        if (!errorSpan) {
            errorSpan = field.parentNode.querySelector('.text-danger');
        }

        if (errorSpan) {
            errorSpan.textContent = message;
        }
    }

    /**
     * Initialize password strength indicator
     */
    function initPasswordStrength() {
        const passwordInput = document.querySelector('input[asp-for="Password"]');
        if (!passwordInput) return;

        // Create password strength indicator
        const strengthContainer = document.createElement('div');
        strengthContainer.className = 'password-strength';

        const strengthBar = document.createElement('div');
        strengthBar.className = 'password-strength-bar';
        strengthContainer.appendChild(strengthBar);

        // Insert after password input
        passwordInput.parentNode.insertBefore(strengthContainer, passwordInput.nextSibling);

        // Add password strength checking
        passwordInput.addEventListener('input', function () {
            const strength = calculatePasswordStrength(this.value);
            updatePasswordStrengthIndicator(strengthBar, strength);
        });
    }

    /**
     * Calculate password strength
     */
    function calculatePasswordStrength(password) {
        let score = 0;

        if (password.length >= 8) score += 1;
        if (/[a-z]/.test(password)) score += 1;
        if (/[A-Z]/.test(password)) score += 1;
        if (/[0-9]/.test(password)) score += 1;
        if (/[^A-Za-z0-9]/.test(password)) score += 1;

        if (password.length >= 12) score += 1;

        return Math.min(score, 3);
    }

    /**
     * Update password strength indicator
     */
    function updatePasswordStrengthIndicator(strengthBar, strength) {
        // Remove existing classes
        strengthBar.className = 'password-strength-bar';

        switch (strength) {
            case 0:
            case 1:
                strengthBar.classList.add('password-strength-weak');
                break;
            case 2:
                strengthBar.classList.add('password-strength-medium');
                break;
            case 3:
                strengthBar.classList.add('password-strength-strong');
                break;
        }
    }

    /**
     * Initialize form submission handling
     */
    function initFormSubmission() {
        const form = document.querySelector('form[asp-action="Register"]');
        const submitButton = document.querySelector('.btn-primary');

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
            }
        });

        // Remove loading state if form validation fails on server side
        window.addEventListener('load', function () {
            if (document.querySelector('.validation-summary')) {
                submitButton.classList.remove('loading');
                submitButton.disabled = false;
            }
        });
    }

    /**
     * Validate all form fields
     */
    function validateAllFields() {
        const form = document.querySelector('form[asp-action="Register"]');
        if (!form) return true;

        const inputs = form.querySelectorAll('input, select');
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
     * Initialize input animations
     */
    function initInputAnimations() {
        const inputs = document.querySelectorAll('.form-control');

        inputs.forEach(input => {
            // Add focus and blur animations
            input.addEventListener('focus', function () {
                this.parentNode.classList.add('focused');
            });

            input.addEventListener('blur', function () {
                this.parentNode.classList.remove('focused');

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
        });
    }

    /**
     * Initialize accessibility features
     */
    function initAccessibility() {
        // Add ARIA labels and descriptions
        const inputs = document.querySelectorAll('.form-control');

        inputs.forEach(input => {
            const label = input.parentNode.querySelector('.form-label');
            const errorSpan = input.parentNode.querySelector('.text-danger');

            if (label && !input.getAttribute('aria-label')) {
                input.setAttribute('aria-label', label.textContent.trim());
            }

            if (errorSpan) {
                const errorId = 'error-' + Math.random().toString(36).substr(2, 9);
                errorSpan.id = errorId;
                input.setAttribute('aria-describedby', errorId);
            }
        });

        // Add role and aria-live for validation messages
        const validationSummary = document.querySelector('.validation-summary');
        if (validationSummary) {
            validationSummary.setAttribute('role', 'alert');
            validationSummary.setAttribute('aria-live', 'polite');
        }
    }

    /**
     * Handle role selection change
     */
    function initRoleSelection() {
        const roleSelect = document.querySelector('select[asp-for="Role"]');
        if (!roleSelect) return;

        roleSelect.addEventListener('change', function () {
            const selectedRole = this.value;

            // Add visual feedback based on role selection
            const card = document.querySelector('.register-card');
            card.classList.remove('role-customer', 'role-organizer');

            if (selectedRole === '3') {
                card.classList.add('role-customer');
            } else if (selectedRole === '2') {
                card.classList.add('role-organizer');
            }
        });
    }

    // Initialize role selection
    initRoleSelection();

    /**
     * Utility function to debounce function calls
     */
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Show success message
     */
    function showSuccessMessage(message) {
        const existingAlert = document.querySelector('.alert-success');
        if (existingAlert) {
            existingAlert.remove();
        }

        const alert = document.createElement('div');
        alert.className = 'alert-success';
        alert.textContent = message;

        const form = document.querySelector('form[asp-action="Register"]');
        form.parentNode.insertBefore(alert, form);

        // Remove after 5 seconds
        setTimeout(() => {
            if (alert.parentNode) {
                alert.remove();
            }
        }, 5000);
    }

    /**
     * Handle form auto-save (optional feature)
     */
    function initAutoSave() {
        const form = document.querySelector('form[asp-action="Register"]');
        if (!form) return;

        const inputs = form.querySelectorAll('input:not([type="password"]), select');

        inputs.forEach(input => {
            input.addEventListener('input', debounce(function () {
                saveFormData(form);
            }, 1000));
        });

        // Load saved data on page load
        loadFormData(form);
    }

    /**
     * Save form data to sessionStorage
     */
    function saveFormData(form) {
        const formData = new FormData(form);
        const data = {};

        for (let [key, value] of formData.entries()) {
            if (!key.includes('Password')) { // Don't save passwords
                data[key] = value;
            }
        }

        try {
            sessionStorage.setItem('registerForm', JSON.stringify(data));
        } catch (e) {
            // Handle storage quota exceeded or other errors
            console.warn('Could not save form data:', e);
        }
    }

    /**
     * Load form data from sessionStorage
     */
    function loadFormData(form) {
        try {
            const savedData = sessionStorage.getItem('registerForm');
            if (!savedData) return;

            const data = JSON.parse(savedData);

            Object.keys(data).forEach(key => {
                const field = form.querySelector(`[name="${key}"]`);
                if (field) {
                    field.value = data[key];
                    if (field.value.trim()) {
                        field.parentNode.classList.add('filled');
                    }
                }
            });
        } catch (e) {
            console.warn('Could not load saved form data:', e);
        }
    }

    // Initialize auto-save (uncomment to enable)
    // initAutoSave();

    // Clear saved data after successful submission
    window.addEventListener('beforeunload', function () {
        const submitButton = document.querySelector('.btn-primary');
        if (submitButton && submitButton.classList.contains('loading')) {
            try {
                sessionStorage.removeItem('registerForm');
            } catch (e) {
                // Handle errors silently
            }
        }
    });

});

// Additional utility functions that can be called from other scripts

/**
 * Manually trigger form validation
 */
function validateRegistrationForm() {
    const form = document.querySelector('form[asp-action="Register"]');
    if (!form) return false;

    const inputs = form.querySelectorAll('input, select');
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
function resetFormValidation() {
    const inputs = document.querySelectorAll('.form-control');
    inputs.forEach(input => {
        input.classList.remove('is-valid', 'is-invalid');
    });
}

/**
 * Focus on first error field
 */
function focusFirstError() {
    const firstError = document.querySelector('.is-invalid');
    if (firstError) {
        firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        firstError.focus();
        return true;
    }
    return false;
}