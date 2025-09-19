document.addEventListener('DOMContentLoaded', function () {

    // Initialize forgot password form enhancements
    initFormValidation();
    initFormSubmission();
    initInputAnimations();
    initAccessibility();
    initSuccessHandling();

    /**
     * Initialize real-time form validation
     */
    function initFormValidation() {
        const form = document.querySelector('form[asp-action="ForgotPassword"]');
        if (!form) return;

        const emailInput = form.querySelector('input[asp-for="Email"]');

        if (emailInput) {
            // Add validation on blur
            emailInput.addEventListener('blur', function () {
                validateEmail(this);
            });

            // Add validation on input for immediate feedback
            emailInput.addEventListener('input', function () {
                // Debounce validation to avoid excessive calls
                clearTimeout(this.validationTimeout);
                this.validationTimeout = setTimeout(() => {
                    validateEmail(this);
                }, 300);
            });

            // Initial validation check if field has value
            if (emailInput.value.trim()) {
                validateEmail(emailInput);
            }
        }
    }

    /**
     * Validate email format and requirements
     */
    function validateEmail(emailField) {
        const email = emailField.value.trim();
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        let isValid = true;
        let message = '';

        // Remove existing validation classes
        emailField.classList.remove('is-valid', 'is-invalid');

        if (!email) {
            // Required field validation
            if (emailField.hasAttribute('required') || emailField.getAttribute('data-val-required')) {
                isValid = false;
                message = 'Email address is required';
            }
        } else {
            // Email format validation
            if (!emailRegex.test(email)) {
                isValid = false;
                message = 'Please enter a valid email address';
            } else if (email.length < 5) {
                isValid = false;
                message = 'Email address is too short';
            } else if (email.length > 254) {
                isValid = false;
                message = 'Email address is too long';
            }
        }

        // Apply validation styles
        if (email && isValid) {
            emailField.classList.add('is-valid');
            hideFieldError(emailField);
        } else if (!isValid) {
            emailField.classList.add('is-invalid');
            showFieldError(emailField, message);
        } else {
            hideFieldError(emailField);
        }

        return isValid;
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
        const form = document.querySelector('form[asp-action="ForgotPassword"]');
        const submitButton = document.querySelector('.forgot-password-btn');

        if (!form || !submitButton) return;

        form.addEventListener('submit', function (e) {
            // Validate email before submission
            const emailInput = form.querySelector('input[asp-for="Email"]');
            const isValid = emailInput ? validateEmail(emailInput) : false;

            if (!isValid) {
                e.preventDefault();

                // Focus on the email field
                if (emailInput) {
                    emailInput.focus();
                    emailInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }

                // Shake the form to indicate error
                const card = document.querySelector('.forgot-password-card');
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
            submitButton.textContent = 'Sending...';

            // Save email for potential success message
            if (emailInput && emailInput.value.trim()) {
                sessionStorage.setItem('forgotPasswordEmail', emailInput.value.trim());
            }
        });

        // Remove loading state if form validation fails on server side
        window.addEventListener('load', function () {
            if (document.querySelector('.alert-danger') || document.querySelector('.text-danger')) {
                submitButton.classList.remove('loading');
                submitButton.disabled = false;
                submitButton.textContent = 'Send Reset Link';
            }
        });
    }

    /**
     * Initialize input animations and interactions
     */
    function initInputAnimations() {
        const emailInput = document.querySelector('input[asp-for="Email"]');
        if (!emailInput) return;

        const inputGroup = emailInput.parentNode;
        const icon = inputGroup.querySelector('.input-icon');

        // Add focus and blur animations
        emailInput.addEventListener('focus', function () {
            inputGroup.classList.add('focused');

            // Add subtle animation to the icon
            if (icon) {
                icon.style.transform = 'translateY(-50%) scale(1.1)';
                icon.style.color = 'var(--primary-color)';
            }
        });

        emailInput.addEventListener('blur', function () {
            inputGroup.classList.remove('focused');

            // Reset icon animation
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

        // Check initial state
        if (emailInput.value.trim()) {
            inputGroup.classList.add('filled');
        }

        // Add typing effect
        emailInput.addEventListener('input', function () {
            if (icon) {
                icon.style.color = 'var(--primary-color)';
            }

            // Animate the input background
            this.style.backgroundColor = '#ffffff';
        });

        // Handle paste events
        emailInput.addEventListener('paste', function (e) {
            setTimeout(() => {
                validateEmail(this);
            }, 100);
        });
    }

    /**
     * Initialize accessibility features
     */
    function initAccessibility() {
        // Add ARIA labels and descriptions
        const emailInput = document.querySelector('input[asp-for="Email"]');

        if (emailInput) {
            const label = emailInput.parentNode.querySelector('.modern-label');
            const errorSpan = emailInput.parentNode.querySelector('.validation-message');

            if (label && !emailInput.getAttribute('aria-label')) {
                emailInput.setAttribute('aria-label', label.textContent.trim());
            }

            if (errorSpan) {
                const errorId = 'error-email-' + Math.random().toString(36).substr(2, 9);
                errorSpan.id = errorId;
                emailInput.setAttribute('aria-describedby', errorId);
            }

            // Add live region for validation feedback
            emailInput.setAttribute('aria-live', 'polite');
        }

        // Add role and aria-live for alert messages
        const alerts = document.querySelectorAll('.alert-modern');
        alerts.forEach(alert => {
            alert.setAttribute('role', 'alert');
            alert.setAttribute('aria-live', 'assertive');
        });

        // Improve keyboard navigation
        const focusableElements = document.querySelectorAll('input, button, a, [tabindex]');
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
        // Check if we're returning from a successful submission
        const urlParams = new URLSearchParams(window.location.search);
        const success = urlParams.get('success');

        if (success === 'true') {
            showSuccessMessage();
        }

        // Handle success message from TempData or ViewBag
        const successAlert = document.querySelector('.alert-success');
        if (successAlert) {
            // Add success animation to the button if it exists
            const submitButton = document.querySelector('.forgot-password-btn');
            if (submitButton) {
                submitButton.classList.add('success');
                setTimeout(() => {
                    submitButton.classList.remove('success');
                }, 600);
            }

            // Auto-hide success message after 10 seconds
            setTimeout(() => {
                if (successAlert.parentNode) {
                    successAlert.style.opacity = '0';
                    setTimeout(() => {
                        if (successAlert.parentNode) {
                            successAlert.remove();
                        }
                    }, 300);
                }
            }, 10000);
        }
    }

    /**
     * Show success message with animation
     */
    function showSuccessMessage() {
        const email = sessionStorage.getItem('forgotPasswordEmail') || 'your email address';

        const message = `
            <i class="fas fa-check-circle"></i>
            <div>
                <strong>Reset link sent!</strong><br>
                Please check your email at <strong>${email}</strong> for instructions to reset your password.
                Don't forget to check your spam folder.
            </div>
        `;

        const alert = document.createElement('div');
        alert.className = 'alert alert-success alert-modern';
        alert.setAttribute('role', 'alert');
        alert.innerHTML = message;

        const form = document.querySelector('form[asp-action="ForgotPassword"]');
        if (form && form.parentNode) {
            form.parentNode.insertBefore(alert, form);
        }

        // Clear the saved email
        sessionStorage.removeItem('forgotPasswordEmail');

        // Focus on the alert for screen readers
        alert.setAttribute('tabindex', '-1');
        alert.focus();
    }

    /**
     * Auto-focus email field
     */
    function autoFocusEmailField() {
        const emailInput = document.querySelector('input[asp-for="Email"]');
        if (emailInput && !emailInput.value.trim()) {
            emailInput.focus();
        }
    }

    // Auto-focus on page load (with slight delay to avoid issues)
    setTimeout(autoFocusEmailField, 100);

    /**
     * Handle Enter key to submit form
     */
    function initKeyboardSubmission() {
        const emailInput = document.querySelector('input[asp-for="Email"]');

        if (emailInput) {
            emailInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    const submitButton = document.querySelector('.forgot-password-btn');
                    if (submitButton && !submitButton.disabled) {
                        submitButton.click();
                    }
                }
            });
        }
    }

    initKeyboardSubmission();

    /**
     * Handle back to login link
     */
    function initBackToLoginLink() {
        const backLink = document.querySelector('a[asp-action="Login"]');

        if (backLink) {
            backLink.addEventListener('click', function (e) {
                // Save current email value for potential return
                const emailInput = document.querySelector('input[asp-for="Email"]');
                if (emailInput && emailInput.value.trim()) {
                    try {
                        sessionStorage.setItem('tempEmail', emailInput.value.trim());
                    } catch (e) {
                        console.warn('Could not save email:', e);
                    }
                }

                // Add loading animation to the link
                const icon = this.querySelector('i');
                if (icon) {
                    icon.style.transform = 'translateX(-5px)';
                }
            });
        }
    }

    initBackToLoginLink();

    /**
     * Load previously entered email from session storage
     */
    function loadPreviousEmail() {
        const emailInput = document.querySelector('input[asp-for="Email"]');

        if (emailInput && !emailInput.value.trim()) {
            try {
                const tempEmail = sessionStorage.getItem('tempEmail');
                if (tempEmail && tempEmail.includes('@')) {
                    emailInput.value = tempEmail;
                    emailInput.parentNode.classList.add('filled');
                    validateEmail(emailInput);
                    sessionStorage.removeItem('tempEmail');
                }
            } catch (e) {
                console.warn('Could not load previous email:', e);
            }
        }
    }

    // Load previous email if available
    setTimeout(loadPreviousEmail, 50);

});

/**
 * Global function to show error message
 */
function showForgotPasswordError(message) {
    const existingAlert = document.querySelector('.alert-danger');
    if (existingAlert) {
        existingAlert.remove();
    }

    const alert = document.createElement('div');
    alert.className = 'alert alert-danger alert-modern';
    alert.setAttribute('role', 'alert');
    alert.innerHTML = `<i class="fas fa-exclamation-circle"></i><div>${message}</div>`;

    const form = document.querySelector('form[asp-action="ForgotPassword"]');
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
function showForgotPasswordSuccess(message, email = null) {
    const existingAlert = document.querySelector('.alert-success');
    if (existingAlert) {
        existingAlert.remove();
    }

    const displayEmail = email || sessionStorage.getItem('forgotPasswordEmail') || 'your email address';

    const alert = document.createElement('div');
    alert.className = 'alert alert-success alert-modern';
    alert.setAttribute('role', 'alert');
    alert.innerHTML = `
        <i class="fas fa-check-circle"></i>
        <div>
            <strong>Reset link sent!</strong><br>
            ${message || `Please check your email at <strong>${displayEmail}</strong> for instructions to reset your password.`}
        </div>
    `;

    const form = document.querySelector('form[asp-action="ForgotPassword"]');
    if (form && form.parentNode) {
        form.parentNode.insertBefore(alert, form);
    }

    // Add success animation to button
    const submitButton = document.querySelector('.forgot-password-btn');
    if (submitButton) {
        submitButton.classList.add('success');
        submitButton.classList.remove('loading');
        submitButton.disabled = false;
        submitButton.textContent = 'Send Reset Link';

        setTimeout(() => {
            submitButton.classList.remove('success');
        }, 600);
    }

    // Focus on the alert for screen readers
    alert.setAttribute('tabindex', '-1');
    alert.focus();

    // Clear saved email
    sessionStorage.removeItem('forgotPasswordEmail');

    // Auto-hide after 12 seconds
    setTimeout(() => {
        if (alert.parentNode) {
            alert.style.opacity = '0';
            setTimeout(() => {
                if (alert.parentNode) {
                    alert.remove();
                }
            }, 300);
        }
    }, 12000);
}

/**
 * Manually trigger form validation (utility function)
 */
function validateForgotPasswordForm() {
    const emailInput = document.querySelector('input[asp-for="Email"]');
    if (!emailInput) return false;

    const email = emailInput.value.trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!email) {
        showForgotPasswordError('Please enter your email address.');
        emailInput.focus();
        return false;
    }

    if (!emailRegex.test(email)) {
        showForgotPasswordError('Please enter a valid email address.');
        emailInput.focus();
        return false;
    }

    return true;
}

/**
 * Reset form validation states
 */
function resetForgotPasswordFormValidation() {
    const emailInput = document.querySelector('input[asp-for="Email"]');
    if (emailInput) {
        emailInput.classList.remove('is-valid', 'is-invalid');
        const errorSpan = emailInput.parentNode.querySelector('.validation-message');
        if (errorSpan) {
            errorSpan.style.display = 'none';
        }
    }

    // Remove any existing alerts
    const alerts = document.querySelectorAll('.alert-modern');
    alerts.forEach(alert => alert.remove());
}

/**
 * Focus on email field
 */
function focusForgotPasswordEmail() {
    const emailInput = document.querySelector('input[asp-for="Email"]');
    if (emailInput) {
        emailInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
        emailInput.focus();
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

/**
 * Handle email suggestions/autocompletion
 */
function initEmailSuggestions() {
    const emailInput = document.querySelector('input[asp-for="Email"]');
    if (!emailInput) return;

    const commonDomains = [
        'gmail.com', 'yahoo.com', 'hotmail.com', 'outlook.com',
        'icloud.com', 'aol.com', 'live.com', 'msn.com'
    ];

    emailInput.addEventListener('input', debounce(function () {
        const value = this.value.trim();
        const atIndex = value.indexOf('@');

        if (atIndex > 0 && atIndex === value.length - 1) {
            // User just typed @, show suggestions
            showEmailSuggestions(this, commonDomains);
        } else if (atIndex > 0) {
            const domain = value.substring(atIndex + 1);
            if (domain.length > 0 && domain.length < 10) {
                const matches = commonDomains.filter(d => d.startsWith(domain.toLowerCase()));
                if (matches.length > 0) {
                    showEmailSuggestions(this, matches, value.substring(0, atIndex + 1));
                }
            }
        }
    }, 300));

    function showEmailSuggestions(input, suggestions, prefix = '') {
        // Remove existing suggestions
        const existingSuggestions = document.querySelector('.email-suggestions');
        if (existingSuggestions) {
            existingSuggestions.remove();
        }

        if (suggestions.length === 0) return;

        const suggestionsList = document.createElement('div');
        suggestionsList.className = 'email-suggestions';
        suggestionsList.style.cssText = `
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            background: white;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
            z-index: 10;
            max-height: 200px;
            overflow-y: auto;
        `;

        suggestions.slice(0, 5).forEach(suggestion => {
            const suggestionItem = document.createElement('div');
            const fullEmail = prefix + suggestion;
            suggestionItem.textContent = fullEmail;
            suggestionItem.style.cssText = `
                padding: 0.75rem 1rem;
                cursor: pointer;
                border-bottom: 1px solid #f3f4f6;
                transition: background-color 0.2s;
            `;

            suggestionItem.addEventListener('mouseenter', function () {
                this.style.backgroundColor = '#f9fafb';
            });

            suggestionItem.addEventListener('mouseleave', function () {
                this.style.backgroundColor = 'transparent';
            });

            suggestionItem.addEventListener('click', function () {
                input.value = fullEmail;
                input.focus();
                suggestionsList.remove();

                // Trigger validation
                const event = new Event('input', { bubbles: true });
                input.dispatchEvent(event);
            });

            suggestionsList.appendChild(suggestionItem);
        });

        input.parentNode.style.position = 'relative';
        input.parentNode.appendChild(suggestionsList);

        // Remove suggestions when clicking outside
        setTimeout(() => {
            document.addEventListener('click', function removeSuggestions(e) {
                if (!suggestionsList.contains(e.target) && e.target !== input) {
                    suggestionsList.remove();
                    document.removeEventListener('click', removeSuggestions);
                }
            });
        }, 100);
    }
}

// Initialize optional features
setTimeout(initVisibilityHandling, 100);

// Uncomment to enable email suggestions
// setTimeout(initEmailSuggestions, 100);

// Handle browser back/forward navigation
window.addEventListener('pageshow', function (event) {
    if (event.persisted) {
        // Reset form state if page was loaded from cache
        const submitButton = document.querySelector('.forgot-password-btn');
        if (submitButton) {
            submitButton.classList.remove('loading');
            submitButton.disabled = false;
            submitButton.textContent = 'Send Reset Link';
        }
        resetForgotPasswordFormValidation();
    }
});

// Prevent form resubmission on page refresh
window.addEventListener('beforeunload', function () {
    const submitButton = document.querySelector('.forgot-password-btn');
    if (submitButton && submitButton.classList.contains('loading')) {
        submitButton.classList.remove('loading');
        submitButton.disabled = false;
        submitButton.textContent = 'Send Reset Link';
    }
});

// Handle network connectivity
window.addEventListener('online', function () {
    const submitButton = document.querySelector('.forgot-password-btn');
    if (submitButton) {
        submitButton.disabled = false;
    }
});

window.addEventListener('offline', function () {
    const submitButton = document.querySelector('.forgot-password-btn');
    if (submitButton) {
        submitButton.disabled = true;
    }

    showForgotPasswordError('No internet connection. Please check your connection and try again.');
});