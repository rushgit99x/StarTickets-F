document.addEventListener('DOMContentLoaded', function () {

    // Initialize reset password form enhancements
    initFormValidation();
    initFormSubmission();
    initInputAnimations();
    initPasswordToggle();
    initPasswordStrength();
    initPasswordMatch();
    initAccessibility();
    initSuccessHandling();

    /**
     * Initialize real-time form validation
     */
    function initFormValidation() {
        const form = document.querySelector('form[asp-action="ResetPassword"]');
        if (!form) return;

        const passwordInput = form.querySelector('input[asp-for="Password"]');
        const confirmPasswordInput = form.querySelector('input[asp-for="ConfirmPassword"]');

        if (passwordInput) {
            // Check initial state
            if (input.value.trim()) {
                inputGroup.classList.add('filled');
            }

            // Add typing effect
            input.addEventListener('input', function () {
                const icon = inputGroup.querySelector('.input-icon');
                if (icon) {
                    icon.style.color = 'var(--primary-color)';
                }
            });
        });
    }

/**
 * Initialize password toggle functionality
 */
function initPasswordToggle() {
    const passwordFields = document.querySelectorAll('input[type="password"]');

    passwordFields.forEach(field => {
        if (field.hasAttribute('readonly')) return;

        const inputGroup = field.parentNode;

        // Create toggle button
        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'password-toggle';
        toggleButton.innerHTML = '<i class="fas fa-eye"></i>';
        toggleButton.setAttribute('aria-label', 'Toggle password visibility');

        // Insert toggle button
        inputGroup.appendChild(toggleButton);

        // Handle toggle functionality
        toggleButton.addEventListener('click', function () {
            const icon = this.querySelector('i');

            if (field.type === 'password') {
                field.type = 'text';
                icon.className = 'fas fa-eye-slash';
                this.setAttribute('aria-label', 'Hide password');
            } else {
                field.type = 'password';
                icon.className = 'fas fa-eye';
                this.setAttribute('aria-label', 'Show password');
            }

            // Maintain focus on the input field
            field.focus();
        });

        // Handle keyboard activation
        toggleButton.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                this.click();
            }
        });
    });
}

/**
 * Initialize password strength indicator
 */
function initPasswordStrength() {
    const passwordField = document.querySelector('input[asp-for="Password"]');
    if (!passwordField) return;

    // Create strength indicator
    const strengthIndicator = document.createElement('div');
    strengthIndicator.className = 'password-strength-indicator';
    strengthIndicator.innerHTML = `
            <div class="password-strength-bar">
                <div class="password-strength-fill"></div>
            </div>
            <div class="password-strength-text">Password strength: <span class="strength-level">Enter password</span></div>
        `;

    // Create requirements list
    const requirementsList = document.createElement('div');
    requirementsList.className = 'password-requirements';
    requirementsList.innerHTML = `
            <div class="password-requirement" data-requirement="minLength">
                <i class="fas fa-circle"></i>
                At least 6 characters
            </div>
            <div class="password-requirement" data-requirement="hasLowercase">
                <i class="fas fa-circle"></i>
                One lowercase letter
            </div>
            <div class="password-requirement" data-requirement="hasUppercase">
                <i class="fas fa-circle"></i>
                One uppercase letter
            </div>
            <div class="password-requirement" data-requirement="hasDigit">
                <i class="fas fa-circle"></i>
                One number
            </div>
            <div class="password-requirement" data-requirement="hasSpecialChar">
                <i class="fas fa-circle"></i>
                One special character (!@#$%^&*)
            </div>
        `;

    // Insert after password field
    const passwordGroup = passwordField.closest('.modern-input-group');
    passwordGroup.appendChild(strengthIndicator);
    passwordGroup.appendChild(requirementsList);

    // Handle password input
    passwordField.addEventListener('input', function () {
        const password = this.value;
        updatePasswordStrength(password, strengthIndicator, requirementsList);

        // Show/hide indicators based on focus and content
        if (password.length > 0) {
            strengthIndicator.style.display = 'block';
            requirementsList.style.display = 'block';
        } else {
            strengthIndicator.style.display = 'none';
            requirementsList.style.display = 'none';
        }
    });

    passwordField.addEventListener('focus', function () {
        if (this.value.length > 0) {
            strengthIndicator.style.display = 'block';
            requirementsList.style.display = 'block';
        }
    });

    passwordField.addEventListener('blur', function () {
        // Keep indicators visible if password doesn't meet all requirements
        const requirements = checkPasswordRequirements(this.value);
        const allMet = Object.values(requirements).every(Boolean);

        if (allMet || this.value.length === 0) {
            setTimeout(() => {
                strengthIndicator.style.display = 'none';
                requirementsList.style.display = 'none';
            }, 200);
        }
    });
}

/**
 * Update password strength indicator
 */
function updatePasswordStrength(password, strengthIndicator, requirementsList) {
    const requirements = checkPasswordRequirements(password);
    const strengthFill = strengthIndicator.querySelector('.password-strength-fill');
    const strengthText = strengthIndicator.querySelector('.strength-level');

    // Calculate strength score
    let score = 0;
    let metCount = 0;

    Object.entries(requirements).forEach(([key, met]) => {
        const requirementEl = requirementsList.querySelector(`[data-requirement="${key}"]`);
        if (requirementEl) {
            const icon = requirementEl.querySelector('i');
            if (met) {
                requirementEl.classList.add('met');
                icon.className = 'fas fa-check-circle';
                metCount++;
            } else {
                requirementEl.classList.remove('met');
                icon.className = 'fas fa-circle';
            }
        }
    });

    // Calculate strength percentage
    const totalRequirements = Object.keys(requirements).length;
    const strengthPercentage = (metCount / totalRequirements) * 100;

    // Update strength bar and text
    strengthFill.style.width = `${strengthPercentage}%`;

    if (metCount < 2) {
        strengthFill.className = 'password-strength-fill strength-weak';
        strengthText.textContent = 'Weak';
        strengthText.className = 'strength-level strength-weak';
    } else if (metCount < 4) {
        strengthFill.className = 'password-strength-fill strength-medium';
        strengthText.textContent = 'Medium';
        strengthText.className = 'strength-level strength-medium';
    } else {
        strengthFill.className = 'password-strength-fill strength-strong';
        strengthText.textContent = 'Strong';
        strengthText.className = 'strength-level strength-strong';
    }
}

/**
 * Initialize password match indicator
 */
function initPasswordMatch() {
    const passwordField = document.querySelector('input[asp-for="Password"]');
    const confirmPasswordField = document.querySelector('input[asp-for="ConfirmPassword"]');

    if (!passwordField || !confirmPasswordField) return;

    // Create match indicator
    const matchIndicator = document.createElement('div');
    matchIndicator.className = 'password-match-indicator';

    // Insert after confirm password field
    const confirmPasswordGroup = confirmPasswordField.closest('.modern-input-group');
    confirmPasswordGroup.appendChild(matchIndicator);

    function updateMatchIndicator() {
        const password = passwordField.value;
        const confirmPassword = confirmPasswordField.value;

        if (confirmPassword.length === 0) {
            matchIndicator.style.display = 'none';
            return;
        }

        matchIndicator.style.display = 'block';

        if (password === confirmPassword && confirmPassword.length > 0) {
            matchIndicator.className = 'password-match-indicator match';
            matchIndicator.innerHTML = '<i class="fas fa-check-circle me-1"></i>Passwords match';
        } else {
            matchIndicator.className = 'password-match-indicator no-match';
            matchIndicator.innerHTML = '<i class="fas fa-times-circle me-1"></i>Passwords do not match';
        }
    }

    passwordField.addEventListener('input', updateMatchIndicator);
    confirmPasswordField.addEventListener('input', updateMatchIndicator);
}

/**
 * Initialize accessibility features
 */
function initAccessibility() {
    // Add ARIA labels and descriptions
    const inputs = document.querySelectorAll('.modern-input');

    inputs.forEach(input => {
        const label = input.closest('.modern-input-group').querySelector('.modern-label');
        const errorSpan = input.parentNode.querySelector('.validation-message');

        if (label && !input.getAttribute('aria-label')) {
            input.setAttribute('aria-label', label.textContent.trim());
        }

        if (errorSpan) {
            const errorId = 'error-' + input.name + '-' + Math.random().toString(36).substr(2, 9);
            errorSpan.id = errorId;
            input.setAttribute('aria-describedby', errorId);
        }
    });

    // Add role and aria-live for alert messages
    const alerts = document.querySelectorAll('.alert-modern');
    alerts.forEach(alert => {
        alert.setAttribute('role', 'alert');
        alert.setAttribute('aria-live', 'assertive');
    });

    // Improve keyboard navigation
    const focusableElements = document.querySelectorAll('input:not([readonly]), button, a, [tabindex]');
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
 * Initialize success handling
 */
function initSuccessHandling() {
    // Check for success message
    const successAlert = document.querySelector('.alert-success');
    if (successAlert) {
        // Add success animation to the button if it exists
        const submitButton = document.querySelector('.reset-password-btn');
        if (submitButton) {
            submitButton.classList.add('success');
            setTimeout(() => {
                submitButton.classList.remove('success');
            }, 600);
        }

        // Auto-redirect to login page after success (optional)
        setTimeout(() => {
            const loginLink = document.querySelector('a[asp-action="Login"]');
            if (loginLink) {
                showResetPasswordSuccess('Password reset successfully! Redirecting to login page...');
                setTimeout(() => {
                    window.location.href = loginLink.href;
                }, 2000);
            }
        }, 1500);
    }
}

/**
 * Auto-focus first empty field
 */
function autoFocusFirstEmptyField() {
    const passwordField = document.querySelector('input[asp-for="Password"]');
    const confirmPasswordField = document.querySelector('input[asp-for="ConfirmPassword"]');

    if (passwordField && !passwordField.value.trim()) {
        passwordField.focus();
    } else if (confirmPasswordField && !confirmPasswordField.value.trim()) {
        confirmPasswordField.focus();
    }
}

// Auto-focus on page load (with slight delay)
setTimeout(autoFocusFirstEmptyField, 100);

/**
 * Handle Enter key to submit form
 */
function initKeyboardSubmission() {
    const inputs = document.querySelectorAll('.modern-input:not([readonly])');

    inputs.forEach(input => {
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const submitButton = document.querySelector('.reset-password-btn');
                if (submitButton && !submitButton.disabled) {
                    submitButton.click();
                }
            }
        });
    });
}

initKeyboardSubmission();
});

/**
 * Global function to show error message
 */
function showResetPasswordError(message) {
    const existingAlert = document.querySelector('.alert-danger');
    if (existingAlert) {
        existingAlert.remove();
    }

    const alert = document.createElement('div');
    alert.className = 'alert alert-danger alert-modern';
    alert.setAttribute('role', 'alert');
    alert.innerHTML = `<i class="fas fa-exclamation-circle"></i><div>${message}</div>`;

    const form = document.querySelector('form[asp-action="ResetPassword"]');
    if (form && form.parentNode) {
        form.parentNode.insertBefore(alert, form);
    }

    // Focus on the alert for screen readers
    alert.setAttribute('tabindex', '-1');
    alert.focus();

    // Remove after 8 seconds
    setTimeout(() => {
        if (alert.parentNode) {
            alert.style.opacity = '0';
            setTimeout(() => {
                if (alert.parentNode) {
                    alert.remove();
                }
            }, 300);
        }
    }, 8000);
}

/**
 * Global function to show success message
 */
function showResetPasswordSuccess(message) {
    const existingAlert = document.querySelector('.alert-success');
    if (existingAlert) {
        existingAlert.remove();
    }

    const alert = document.createElement('div');
    alert.className = 'alert alert-success alert-modern';
    alert.setAttribute('role', 'alert');
    alert.innerHTML = `<i class="fas fa-check-circle"></i><div>${message}</div>`;

    const form = document.querySelector('form[asp-action="ResetPassword"]');
    if (form && form.parentNode) {
        form.parentNode.insertBefore(alert, form);
    }

    // Add success animation to button
    const submitButton = document.querySelector('.reset-password-btn');
    if (submitButton) {
        submitButton.classList.add('success');
        submitButton.classList.remove('loading');
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fas fa-key me-2"></i>Reset Password';

        setTimeout(() => {
            submitButton.classList.remove('success');
        }, 600);
    }

    // Focus on the alert for screen readers
    alert.setAttribute('tabindex', '-1');
    alert.focus();
}

/**
 * Manually trigger form validation (utility function)
 */
function validateResetPasswordForm() {
    const passwordField = document.querySelector('input[asp-for="Password"]');
    const confirmPasswordField = document.querySelector('input[asp-for="ConfirmPassword"]');

    let isValid = true;

    if (passwordField) {
        const password = passwordField.value.trim();
        if (!password) {
            showResetPasswordError('Please enter a new password.');
            passwordField.focus();
            return false;
        }

        const requirements = checkPasswordRequirements(password);
        if (!Object.values(requirements).every(Boolean)) {
            showResetPasswordError('Password does not meet the security requirements.');
            passwordField.focus();
            return false;
        }
    }

    if (confirmPasswordField) {
        const confirmPassword = confirmPasswordField.value.trim();
        const password = passwordField ? passwordField.value.trim() : '';

        if (!confirmPassword) {
            showResetPasswordError('Please confirm your new password.');
            confirmPasswordField.focus();
            return false;
        }

        if (password !== confirmPassword) {
            showResetPasswordError('Passwords do not match.');
            confirmPasswordField.focus();
            return false;
        }
    }

    return isValid;
}

/**
 * Reset form validation states
 */
function resetResetPasswordFormValidation() {
    const inputs = document.querySelectorAll('.modern-input:not([readonly])');
    inputs.forEach(input => {
        input.classList.remove('is-valid', 'is-invalid');
        const errorSpan = input.parentNode.querySelector('.validation-message');
        if (errorSpan) {
            errorSpan.style.display = 'none';
        }
    });

    // Remove any existing alerts
    const alerts = document.querySelectorAll('.alert-modern');
    alerts.forEach(alert => alert.remove());

    // Hide password indicators
    const indicators = document.querySelectorAll('.password-strength-indicator, .password-requirements, .password-match-indicator');
    indicators.forEach(indicator => {
        indicator.style.display = 'none';
    });
}

/**
 * Focus on first error field
 */
function focusFirstResetPasswordError() {
    const firstError = document.querySelector('.modern-input.is-invalid');
    if (firstError) {
        firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        firstError.focus();
        return true;
    }
    return false;
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
setTimeout(initVisibilityHandling, 100);

// Handle browser back/forward navigation
window.addEventListener('pageshow', function (event) {
    if (event.persisted) {
        // Reset form state if page was loaded from cache
        const submitButton = document.querySelector('.reset-password-btn');
        if (submitButton) {
            submitButton.classList.remove('loading');
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="fas fa-key me-2"></i>Reset Password';
        }
        resetResetPasswordFormValidation();
    }
});

// Prevent form resubmission on page refresh
window.addEventListener('beforeunload', function () {
    const submitButton = document.querySelector('.reset-password-btn');
    if (submitButton && submitButton.classList.contains('loading')) {
        submitButton.classList.remove('loading');
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fas fa-key me-2"></i>Reset Password';
    }
});

// Handle network connectivity
window.addEventListener('online', function () {
    const submitButton = document.querySelector('.reset-password-btn');
    if (submitButton) {
        submitButton.disabled = false;
    }
});

window.addEventListener('offline', function () {
    const submitButton = document.querySelector('.reset-password-btn');
    if (submitButton) {
        submitButton.disabled = true;
    }

    showResetPasswordError('No internet connection. Please check your connection and try again.');
}); Add validation on blur
passwordInput.addEventListener('blur', function () {
    validatePassword(this);
});

// Add validation on input for immediate feedback
passwordInput.addEventListener('input', function () {
    clearTimeout(this.validationTimeout);
    this.validationTimeout = setTimeout(() => {
        validatePassword(this);
    }, 300);
});
        }

if (confirmPasswordInput) {
    // Add validation on blur
    confirmPasswordInput.addEventListener('blur', function () {
        validateConfirmPassword(this);
    });

    // Add validation on input for immediate feedback
    confirmPasswordInput.addEventListener('input', function () {
        clearTimeout(this.validationTimeout);
        this.validationTimeout = setTimeout(() => {
            validateConfirmPassword(this);
        }, 300);
    });
}
    }

/**
 * Validate password requirements
 */
function validatePassword(passwordField) {
    const password = passwordField.value;
    let isValid = true;
    let message = '';

    // Remove existing validation classes
    passwordField.classList.remove('is-valid', 'is-invalid');

    if (!password) {
        if (passwordField.hasAttribute('required') || passwordField.getAttribute('data-val-required')) {
            isValid = false;
            message = 'Password is required';
        }
    } else {
        // Check password requirements
        const requirements = checkPasswordRequirements(password);

        if (password.length < 6) {
            isValid = false;
            message = 'Password must be at least 6 characters long';
        } else if (!requirements.hasLowercase) {
            isValid = false;
            message = 'Password must contain at least one lowercase letter';
        } else if (!requirements.hasUppercase) {
            isValid = false;
            message = 'Password must contain at least one uppercase letter';
        } else if (!requirements.hasDigit) {
            isValid = false;
            message = 'Password must contain at least one number';
        } else if (!requirements.hasSpecialChar) {
            isValid = false;
            message = 'Password must contain at least one special character';
        }
    }

    // Apply validation styles
    if (password && isValid) {
        passwordField.classList.add('is-valid');
        hideFieldError(passwordField);
    } else if (!isValid && password) {
        passwordField.classList.add('is-invalid');
        showFieldError(passwordField, message);
    } else {
        hideFieldError(passwordField);
    }

    // Update confirm password validation if it has a value
    const confirmPasswordField = document.querySelector('input[asp-for="ConfirmPassword"]');
    if (confirmPasswordField && confirmPasswordField.value) {
        validateConfirmPassword(confirmPasswordField);
    }

    return isValid;
}

/**
 * Validate password confirmation
 */
function validateConfirmPassword(confirmPasswordField) {
    const confirmPassword = confirmPasswordField.value;
    const passwordField = document.querySelector('input[asp-for="Password"]');
    const password = passwordField ? passwordField.value : '';

    let isValid = true;
    let message = '';

    // Remove existing validation classes
    confirmPasswordField.classList.remove('is-valid', 'is-invalid');

    if (!confirmPassword) {
        if (confirmPasswordField.hasAttribute('required') || confirmPasswordField.getAttribute('data-val-required')) {
            isValid = false;
            message = 'Please confirm your password';
        }
    } else if (password !== confirmPassword) {
        isValid = false;
        message = 'Passwords do not match';
    }

    // Apply validation styles
    if (confirmPassword && isValid) {
        confirmPasswordField.classList.add('is-valid');
        hideFieldError(confirmPasswordField);
    } else if (!isValid && confirmPassword) {
        confirmPasswordField.classList.add('is-invalid');
        showFieldError(confirmPasswordField, message);
    } else {
        hideFieldError(confirmPasswordField);
    }

    return isValid;
}

/**
 * Check password requirements
 */
function checkPasswordRequirements(password) {
    return {
        hasLowercase: /[a-z]/.test(password),
        hasUppercase: /[A-Z]/.test(password),
        hasDigit: /\d/.test(password),
        hasSpecialChar: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password),
        minLength: password.length >= 6,
        maxLength: password.length <= 100
    };
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
    const form = document.querySelector('form[asp-action="ResetPassword"]');
    const submitButton = document.querySelector('.reset-password-btn');

    if (!form || !submitButton) return;

    form.addEventListener('submit', function (e) {
        // Validate all fields before submission
        const isValid = validateAllFields();

        if (!isValid) {
            e.preventDefault();

            // Add loading state briefly to show feedback
            submitButton.classList.add('loading');
            setTimeout(() => {
                submitButton.classList.remove('loading');
            }, 500);

            // Scroll to first error
            const firstError = document.querySelector('.is-invalid');
            if (firstError) {
                firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                firstError.focus();
            }

            // Shake the form to indicate error
            const card = document.querySelector('.reset-password-card');
            if (card) {
                card.style.animation = 'shake 0.5s ease-out';
                setTimeout(() => {
                    card.style.animation = '';
                }, 500);
            }

            return;
        }

        // Add loading state to button
        submitButton.classList.add('loading');
        submitButton.disabled = true;
        submitButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Resetting...';
    });

    // Remove loading state if form validation fails on server side
    window.addEventListener('load', function () {
        if (document.querySelector('.alert-danger') || document.querySelector('.text-danger')) {
            submitButton.classList.remove('loading');
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="fas fa-key me-2"></i>Reset Password';
        }
    });
}

/**
 * Validate all form fields
 */
function validateAllFields() {
    const passwordField = document.querySelector('input[asp-for="Password"]');
    const confirmPasswordField = document.querySelector('input[asp-for="ConfirmPassword"]');

    let isValid = true;

    if (passwordField) {
        if (!validatePassword(passwordField)) {
            isValid = false;
        }
    }

    if (confirmPasswordField) {
        if (!validateConfirmPassword(confirmPasswordField)) {
            isValid = false;
        }
    }

    return isValid;
}

/**
 * Initialize input animations and interactions
 */
function initInputAnimations() {
    const inputs = document.querySelectorAll('.modern-input:not([readonly])');

    inputs.forEach(input => {
        const inputGroup = input.parentNode.parentNode;

        // Add focus and blur animations
        input.addEventListener('focus', function () {
            inputGroup.classList.add('focused');

            // Add subtle animation to the icon
            const icon = inputGroup.querySelector('.input-icon');
            if (icon) {
                icon.style.transform = 'translateY(-50%) scale(1.1)';
                icon.style.color = 'var(--primary-color)';
            }
        });

        input.addEventListener('blur', function () {
            inputGroup.classList.remove('focused');

            // Reset icon animation
            const icon = inputGroup.querySelector('.input-icon');
            if (icon) {
                icon.style.transform = 'translateY(-50%) scale(1)';
            }

            // Add filled class if input has value
            if (this.value.trim()) {
                inputGroup.classList.add('filled');
            } else {
                inputGroup.classList.remove('filled');
            }
        });
