document.addEventListener('DOMContentLoaded', function () {
    initContactForm();
    initFAQ();
    initAnimations();
});

function initContactForm() {
    const form = document.getElementById('contact-form');
    const submitBtn = document.getElementById('submit-btn');
    const messageTextarea = document.querySelector('textarea[name="Message"]');
    const charCount = document.getElementById('char-count');

    // Character counter
    if (messageTextarea && charCount) {
        messageTextarea.addEventListener('input', function () {
            const count = this.value.length;
            charCount.textContent = count;

            if (count > 800) {
                charCount.style.color = 'var(--error-color)';
            } else if (count > 600) {
                charCount.style.color = 'var(--secondary-color)';
            } else {
                charCount.style.color = 'var(--text-muted)';
            }
        });
    }

    // Form submission
    if (form) {
        form.addEventListener('submit', function (e) {
            if (!validateForm()) {
                e.preventDefault();
                return false;
            }

            // Show loading state
            const btnText = submitBtn.querySelector('.btn-text');
            const btnLoading = submitBtn.querySelector('.btn-loading');

            btnText.style.display = 'none';
            btnLoading.style.display = 'inline-flex';
            submitBtn.classList.add('loading');

            showNotification('Sending your message...', 'info');
        });
    }

    // Real-time validation
    const inputs = form.querySelectorAll('input, select, textarea');
    inputs.forEach(input => {
        input.addEventListener('blur', function () {
            validateField(this);
        });

        input.addEventListener('input', function () {
            clearFieldError(this);
        });
    });
}

function validateForm() {
    const form = document.getElementById('contact-form');
    let isValid = true;

    const requiredFields = form.querySelectorAll('input[required], select[required], textarea[required]');

    requiredFields.forEach(field => {
        if (!validateField(field)) {
            isValid = false;
        }
    });

    return isValid;
}

function validateField(field) {
    const value = field.value.trim();
    const fieldName = field.name;
    let isValid = true;
    let errorMessage = '';

    // Required validation
    if (field.hasAttribute('required') && !value) {
        errorMessage = 'This field is required.';
        isValid = false;
    }

    // Email validation
    else if (fieldName === 'Email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            errorMessage = 'Please enter a valid email address.';
            isValid = false;
        }
    }

    // Message length validation
    else if (fieldName === 'Message' && value && value.length > 1000) {
        errorMessage = 'Message cannot exceed 1000 characters.';
        isValid = false;
    }

    // Show/hide error
    const errorElement = field.parentNode.querySelector('.validation-message');
    if (errorElement) {
        if (!isValid) {
            errorElement.textContent = errorMessage;
            errorElement.style.display = 'block';
            field.classList.add('error');
        } else {
            errorElement.textContent = '';
            errorElement.style.display = 'none';
            field.classList.remove('error');
        }
    }

    return isValid;
}

function clearFieldError(field) {
    const errorElement = field.parentNode.querySelector('.validation-message');
    if (errorElement && field.value.trim()) {
        errorElement.style.display = 'none';
        field.classList.remove('error');
    }
}

function initFAQ() {
    const faqItems = document.querySelectorAll('.faq-item');

    faqItems.forEach(item => {
        const question = item.querySelector('.faq-question');

        question.addEventListener('click', function () {
            const isActive = item.classList.contains('active');

            // Close all other items
            faqItems.forEach(otherItem => {
                if (otherItem !== item) {
                    otherItem.classList.remove('active');
                }
            });

            // Toggle current item
            item.classList.toggle('active', !isActive);
        });
    });
}

function initAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    const animateElements = document.querySelectorAll(
        '.contact-card, .faq-item, .slide-in-left, .slide-in-right'
    );

    animateElements.forEach(el => {
        observer.observe(el);
    });
}

function openMap() {
    const address = encodeURIComponent('123 Event Avenue, Colombo, Sri Lanka');
    const googleMapsUrl = 'https://www.google.com/maps/search/?api=1&query=' + address;
    window.open(googleMapsUrl, '_blank');
}

function showNotification(message, type) {
    type = type || 'info';

    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => {
        notification.remove();
    });

    const notification = document.createElement('div');
    notification.className = 'notification notification-' + type;

    const icon = type === 'success' ? 'check-circle' :
        type === 'error' ? 'exclamation-circle' :
            type === 'info' ? 'info-circle' : 'bell';

    notification.innerHTML =
        '<i class="fas fa-' + icon + '"></i>' +
        '<span>' + message + '</span>';

    // Add notification styles if not already present
    if (!document.querySelector('#notification-styles')) {
        const styleSheet = document.createElement('style');
        styleSheet.id = 'notification-styles';
        styleSheet.textContent =
            '.notification {' +
            'position: fixed;' +
            'top: 100px;' +
            'right: 20px;' +
            'padding: 15px 20px;' +
            'border-radius: 10px;' +
            'color: white;' +
            'font-weight: 500;' +
            'z-index: 10001;' +
            'animation: slideInRight 0.3s ease, fadeOut 0.3s ease 4.7s;' +
            'max-width: 350px;' +
            'word-wrap: break-word;' +
            'display: flex;' +
            'align-items: center;' +
            'gap: 10px;' +
            'box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);' +
            '}' +
            '.notification-success {' +
            'background: linear-gradient(135deg, #10b981, #059669);' +
            '}' +
            '.notification-error {' +
            'background: linear-gradient(135deg, #ef4444, #dc2626);' +
            '}' +
            '.notification-info {' +
            'background: linear-gradient(135deg, #3b82f6, #2563eb);' +
            '}' +
            '.notification i {' +
            'font-size: 18px;' +
            'flex-shrink: 0;' +
            '}' +
            '@keyframes slideInRight {' +
            'from { transform: translateX(100%); opacity: 0; }' +
            'to { transform: translateX(0); opacity: 1; }' +
            '}' +
            '@keyframes fadeOut {' +
            'from { opacity: 1; }' +
            'to { opacity: 0; }' +
            '}';
        document.head.appendChild(styleSheet);
    }

    document.body.appendChild(notification);

    // Remove after 5 seconds
    setTimeout(function () {
        if (document.body.contains(notification)) {
            notification.style.animation = 'fadeOut 0.3s ease forwards';
            setTimeout(function () {
                if (document.body.contains(notification)) {
                    document.body.removeChild(notification);
                }
            }, 300);
        }
    }, 5000);
}

// Enhanced form handling for better UX
document.addEventListener('DOMContentLoaded', function () {
    // Add smooth focus transitions
    const formFields = document.querySelectorAll('.form-input, .form-select, .form-textarea');

    formFields.forEach(function (field) {
        field.addEventListener('focus', function () {
            this.parentNode.classList.add('focused');
        });

        field.addEventListener('blur', function () {
            this.parentNode.classList.remove('focused');
            if (!this.value.trim()) {
                this.parentNode.classList.remove('filled');
            } else {
                this.parentNode.classList.add('filled');
            }
        });

        // Check if field has value on page load
        if (field.value.trim()) {
            field.parentNode.classList.add('filled');
        }
    });

    // Auto-resize textarea
    const messageTextarea = document.querySelector('textarea[name="Message"]');
    if (messageTextarea) {
        messageTextarea.addEventListener('input', function () {
            this.style.height = 'auto';
            this.style.height = Math.min(this.scrollHeight, 200) + 'px';
        });
    }

    // Prevent form resubmission on page reload
    if (window.history.replaceState) {
        window.history.replaceState(null, null, window.location.href);
    }

    // Add keyboard shortcuts
    document.addEventListener('keydown', function (e) {
        // Escape key to clear form
        if (e.key === 'Escape' && e.ctrlKey) {
            const form = document.getElementById('contact-form');
            if (form && confirm('Are you sure you want to clear the form?')) {
                form.reset();
                formFields.forEach(function (field) {
                    field.parentNode.classList.remove('filled', 'focused');
                });
                showNotification('Form cleared', 'info');
            }
        }

        // Ctrl+Enter to submit form
        if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
            const submitBtn = document.getElementById('submit-btn');
            if (submitBtn && !submitBtn.classList.contains('loading')) {
                submitBtn.click();
            }
        }
    });

    // Add additional form field styling
    const additionalStyles = document.createElement('style');
    additionalStyles.textContent =
        '.form-group.focused .form-label { color: var(--primary-color); }' +
        '.form-group.filled .form-label i { transform: scale(1.1); }' +
        '.form-group { position: relative; }' +
        '.form-group.focused::after {' +
        'content: "";' +
        'position: absolute;' +
        'bottom: 0;' +
        'left: 0;' +
        'right: 0;' +
        'height: 2px;' +
        'background: var(--gradient-primary);' +
        'border-radius: 1px;' +
        'animation: expandWidth 0.3s ease;' +
        '}' +
        '@keyframes expandWidth {' +
        'from { transform: scaleX(0); }' +
        'to { transform: scaleX(1); }' +
        '}';
    document.head.appendChild(additionalStyles);
});