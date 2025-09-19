// EventDetails.js - JavaScript for Event Details Page

document.addEventListener('DOMContentLoaded', function () {
    // Initialize the page
    initializeEventDetails();

    // Add smooth animations to stats cards
    animateStatsCards();

    // Add interactive hover effects
    addInteractiveEffects();

    // Initialize tooltips if any
    initializeTooltips();

    // Add keyboard navigation support
    addKeyboardNavigation();
});

/**
 * Initialize the event details page
 */
function initializeEventDetails() {
    // Animate progress bars on page load
    animateProgressBars();

    // Format numbers with animations
    animateNumbers();

    // Add fade-in animations to cards
    addFadeInAnimations();

    // Initialize any chart components (if needed in future)
    // initializeCharts();

    console.log('Event Details page initialized successfully');
}

/**
 * Animate progress bars with smooth transitions
 */
function animateProgressBars() {
    const progressBars = document.querySelectorAll('.progress-bar');

    progressBars.forEach((bar, index) => {
        const targetWidth = bar.style.width;
        bar.style.width = '0%';

        setTimeout(() => {
            bar.style.transition = 'width 1.5s ease-out';
            bar.style.width = targetWidth;
        }, 500 + (index * 200)); // Staggered animation
    });
}

/**
 * Animate numbers counting up
 */
function animateNumbers() {
    const numberElements = document.querySelectorAll('.stat-value');

    numberElements.forEach((element) => {
        const targetText = element.textContent;
        const targetNumber = parseFloat(targetText.replace(/[^0-9.-]/g, ''));

        if (!isNaN(targetNumber) && targetNumber > 0) {
            animateNumberCountUp(element, targetNumber, targetText);
        }
    });
}

/**
 * Animate a number counting up to its target value
 * @param {HTMLElement} element - The element to animate
 * @param {number} targetNumber - The target number
 * @param {string} originalText - The original text format
 */
function animateNumberCountUp(element, targetNumber, originalText) {
    const duration = 2000; // 2 seconds
    const startTime = Date.now();
    const isDecimal = originalText.includes('.');
    const isDollar = originalText.includes('$');
    const isPercentage = originalText.includes('%');

    function updateNumber() {
        const elapsed = Date.now() - startTime;
        const progress = Math.min(elapsed / duration, 1);

        // Use easing function for smooth animation
        const easedProgress = easeOutQuart(progress);
        const currentNumber = Math.floor(targetNumber * easedProgress);

        let displayText = currentNumber.toString();

        if (isDecimal && progress === 1) {
            displayText = targetNumber.toFixed(2);
        } else if (isDollar) {
            displayText = currentNumber.toLocaleString();
        }

        if (isDollar) displayText = '$' + displayText;
        if (isPercentage) displayText = displayText + '%';

        element.textContent = displayText;

        if (progress < 1) {
            requestAnimationFrame(updateNumber);
        } else {
            element.textContent = originalText; // Ensure final value is exact
        }
    }

    // Start animation after a delay
    setTimeout(updateNumber, 800);
}

/**
 * Easing function for smooth animations
 * @param {number} t - Progress value (0 to 1)
 * @returns {number} - Eased value
 */
function easeOutQuart(t) {
    return 1 - Math.pow(1 - t, 4);
}

/**
 * Add fade-in animations to cards
 */
function addFadeInAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in-visible');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    // Add fade-in class to elements
    const elementsToAnimate = document.querySelectorAll('.detail-card, .main-card, .stat-card');
    elementsToAnimate.forEach((element, index) => {
        element.classList.add('fade-in');
        element.style.animationDelay = `${index * 0.1}s`;
        observer.observe(element);
    });

    // Add CSS for fade-in animation
    addFadeInCSS();
}

/**
 * Add CSS for fade-in animations
 */
function addFadeInCSS() {
    const style = document.createElement('style');
    style.textContent = `
        .fade-in {
            opacity: 0;
            transform: translateY(20px);
            transition: opacity 0.6s ease-out, transform 0.6s ease-out;
        }
        
        .fade-in-visible {
            opacity: 1;
            transform: translateY(0);
        }
    `;
    document.head.appendChild(style);
}

/**
 * Animate stats cards on hover
 */
function animateStatsCards() {
    const statCards = document.querySelectorAll('.stat-card');

    statCards.forEach((card) => {
        card.addEventListener('mouseenter', function () {
            // Add subtle bounce animation
            this.style.transform = 'translateY(-5px) scale(1.02)';
            this.style.transition = 'transform 0.3s cubic-bezier(0.4, 0, 0.2, 1)';

            // Animate the icon
            const icon = this.querySelector('.stat-icon i');
            if (icon) {
                icon.style.transform = 'scale(1.1) rotate(5deg)';
                icon.style.transition = 'transform 0.3s ease';
            }
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0) scale(1)';

            const icon = this.querySelector('.stat-icon i');
            if (icon) {
                icon.style.transform = 'scale(1) rotate(0deg)';
            }
        });
    });
}

/**
 * Add interactive effects to various elements
 */
function addInteractiveEffects() {
    // Ticket category hover effects
    const ticketCategories = document.querySelectorAll('.ticket-category-item');
    ticketCategories.forEach((category) => {
        category.addEventListener('mouseenter', function () {
            const progressBar = this.querySelector('.progress-bar');
            if (progressBar) {
                progressBar.style.filter = 'brightness(1.2)';
                progressBar.style.boxShadow = '0 0 15px rgba(34, 197, 94, 0.5)';
            }
        });

        category.addEventListener('mouseleave', function () {
            const progressBar = this.querySelector('.progress-bar');
            if (progressBar) {
                progressBar.style.filter = 'brightness(1)';
                progressBar.style.boxShadow = 'none';
            }
        });
    });

    // Table row hover effects
    const tableRows = document.querySelectorAll('.bookings-table tbody tr');
    tableRows.forEach((row) => {
        row.addEventListener('mouseenter', function () {
            this.style.transform = 'scale(1.01)';
            this.style.transition = 'transform 0.2s ease, background-color 0.3s ease';
        });

        row.addEventListener('mouseleave', function () {
            this.style.transform = 'scale(1)';
        });
    });

    // Button ripple effects
    addRippleEffect();
}

/**
 * Add ripple effect to buttons
 */
function addRippleEffect() {
    const buttons = document.querySelectorAll('.btn');

    buttons.forEach((button) => {
        button.addEventListener('click', function (e) {
            // Remove any existing ripple
            const existingRipple = this.querySelector('.ripple');
            if (existingRipple) {
                existingRipple.remove();
            }

            // Create ripple element
            const ripple = document.createElement('span');
            ripple.classList.add('ripple');

            // Calculate position and size
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;

            ripple.style.cssText = `
                position: absolute;
                border-radius: 50%;
                background: rgba(255, 255, 255, 0.3);
                transform: scale(0);
                animation: ripple-animation 0.6s linear;
                left: ${x}px;
                top: ${y}px;
                width: ${size}px;
                height: ${size}px;
                pointer-events: none;
            `;

            this.appendChild(ripple);

            // Remove ripple after animation
            setTimeout(() => {
                ripple.remove();
            }, 600);
        });

        // Ensure button has relative positioning
        if (window.getComputedStyle(button).position === 'static') {
            button.style.position = 'relative';
        }
        button.style.overflow = 'hidden';
    });

    // Add CSS for ripple animation
    addRippleCSS();
}

/**
 * Add CSS for ripple animations
 */
function addRippleCSS() {
    const style = document.createElement('style');
    style.textContent = `
        @keyframes ripple-animation {
            to {
                transform: scale(4);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
}

/**
 * Initialize tooltips for elements with title attributes
 */
function initializeTooltips() {
    const elementsWithTooltips = document.querySelectorAll('[title]');

    elementsWithTooltips.forEach((element) => {
        const tooltipText = element.getAttribute('title');
        element.removeAttribute('title'); // Remove default tooltip

        element.addEventListener('mouseenter', function (e) {
            showCustomTooltip(e.target, tooltipText);
        });

        element.addEventListener('mouseleave', function () {
            hideCustomTooltip();
        });
    });
}

/**
 * Show custom tooltip
 * @param {HTMLElement} element - Element to show tooltip for
 * @param {string} text - Tooltip text
 */
function showCustomTooltip(element, text) {
    // Remove existing tooltip
    hideCustomTooltip();

    const tooltip = document.createElement('div');
    tooltip.className = 'custom-tooltip';
    tooltip.textContent = text;
    tooltip.style.cssText = `
        position: absolute;
        background: linear-gradient(135deg, #1e293b, #334155);
        color: #e2e8f0;
        padding: 8px 12px;
        border-radius: 8px;
        font-size: 0.875rem;
        border: 1px solid #475569;
        z-index: 10000;
        pointer-events: none;
        opacity: 0;
        transform: translateY(-5px);
        transition: opacity 0.3s ease, transform 0.3s ease;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
        max-width: 200px;
        word-wrap: break-word;
    `;

    document.body.appendChild(tooltip);

    // Position tooltip
    const rect = element.getBoundingClientRect();
    tooltip.style.left = rect.left + rect.width / 2 - tooltip.offsetWidth / 2 + 'px';
    tooltip.style.top = rect.top - tooltip.offsetHeight - 10 + 'px';

    // Show tooltip with animation
    setTimeout(() => {
        tooltip.style.opacity = '1';
        tooltip.style.transform = 'translateY(0)';
    }, 10);
}

/**
 * Hide custom tooltip
 */
function hideCustomTooltip() {
    const tooltip = document.querySelector('.custom-tooltip');
    if (tooltip) {
        tooltip.style.opacity = '0';
        tooltip.style.transform = 'translateY(-5px)';
        setTimeout(() => {
            tooltip.remove();
        }, 300);
    }
}

/**
 * Add keyboard navigation support
 */
function addKeyboardNavigation() {
    // Add focus styles for keyboard navigation
    const style = document.createElement('style');
    style.textContent = `
        .btn:focus,
        .ticket-category-item:focus,
        .bookings-table tbody tr:focus {
            outline: 2px solid #60a5fa;
            outline-offset: 2px;
        }
        
        .ticket-category-item {
            cursor: pointer;
        }
    `;
    document.head.appendChild(style);

    // Make ticket categories focusable
    const ticketCategories = document.querySelectorAll('.ticket-category-item');
    ticketCategories.forEach((category) => {
        category.setAttribute('tabindex', '0');

        category.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                // Could add click-like behavior here if needed
                category.click();
            }
        });
    });

    // Add keyboard support for table rows
    const tableRows = document.querySelectorAll('.bookings-table tbody tr');
    tableRows.forEach((row) => {
        row.setAttribute('tabindex', '0');

        row.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                // Could add detail view navigation here
                console.log('Table row selected:', this);
            }
        });
    });
}

/**
 * Utility function to format currency
 * @param {number} amount - Amount to format
 * @returns {string} - Formatted currency string
 */
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
    }).format(amount);
}

/**
 * Utility function to format percentage
 * @param {number} value - Value to format as percentage
 * @param {number} decimals - Number of decimal places
 * @returns {string} - Formatted percentage string
 */
function formatPercentage(value, decimals = 1) {
    return value.toFixed(decimals) + '%';
}

/**
 * Utility function to debounce function calls
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} - Debounced function
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

// Export functions for potential use in other scripts
window.EventDetailsUtils = {
    formatCurrency,
    formatPercentage,
    debounce
};