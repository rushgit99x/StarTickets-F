// Sidebar functionality
const sidebar = document.getElementById('sidebar');
const mainContent = document.getElementById('mainContent');
const sidebarToggle = document.getElementById('sidebarToggle');

// Toggle sidebar
sidebarToggle.addEventListener('click', function () {
    sidebar.classList.toggle('collapsed');
    mainContent.classList.toggle('expanded');
});

// Navigation functionality
const menuLinks = document.querySelectorAll('.menu-link[data-page]');
const pages = document.querySelectorAll('.page-content');
const quickActionBtns = document.querySelectorAll('.quick-action-btn[data-action]');

// Handle menu navigation
menuLinks.forEach(link => {
    link.addEventListener('click', function (e) {
        e.preventDefault();

        // Remove active class from all links
        menuLinks.forEach(l => l.classList.remove('active'));

        // Add active class to clicked link
        this.classList.add('active');

        // Hide all pages
        pages.forEach(page => page.style.display = 'none');

        // Show selected page
        const targetPage = this.getAttribute('data-page') + '-page';
        const targetElement = document.getElementById(targetPage);
        if (targetElement) {
            targetElement.style.display = 'block';
        }
    });
});

// Handle quick action buttons
quickActionBtns.forEach(btn => {
    btn.addEventListener('click', function (e) {
        e.preventDefault();

        const action = this.getAttribute('data-action');

        // Remove active from all menu links first
        menuLinks.forEach(l => l.classList.remove('active'));

        // Find corresponding menu link and activate it
        const correspondingMenuLink = document.querySelector(`.menu-link[data-page="${action}"]`);
        if (correspondingMenuLink) {
            correspondingMenuLink.classList.add('active');
        }

        // Hide all pages
        pages.forEach(page => page.style.display = 'none');

        // Show target page
        const targetPage = action + '-page';
        const targetElement = document.getElementById(targetPage);
        if (targetElement) {
            targetElement.style.display = 'block';
        }
    });
});

// Counter animation for stats
function animateCounter(element) {
    const target = parseFloat(element.dataset.target.replace(/[,$%]/g, ''));
    const isPercentage = element.dataset.target.includes('%');
    const isCurrency = element.dataset.target.includes('$');
    const increment = target / 100;
    let current = 0;

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            current = target;
            clearInterval(timer);
        }

        let displayValue = Math.floor(current);

        if (isCurrency) {
            element.textContent = '$' + displayValue.toLocaleString();
        } else if (isPercentage) {
            element.textContent = displayValue + '%';
        } else {
            element.textContent = displayValue.toLocaleString();
        }
    }, 20);
}

// Initialize dashboard
document.addEventListener('DOMContentLoaded', function () {
    // Animate counters after page load
    setTimeout(() => {
        const counters = document.querySelectorAll('.stats-value[data-target]');
        counters.forEach(counter => animateCounter(counter));
    }, 500);

    // Initialize charts
    initializeCharts();

    // Set up real-time notifications (simulation)
    simulateRealTimeNotifications();
});

// Chart initialization
function initializeCharts() {
    // Revenue Chart
    const revenueCtx = document.getElementById('revenueChart');
    if (revenueCtx && typeof Chart !== 'undefined') {
        new Chart(revenueCtx, {
            type: 'line',
            data: {
                labels: ['Apr 2025', 'May 2025', 'Jun 2025', 'Jul 2025', 'Aug 2025', 'Sep 2025'],
                datasets: [{
                    label: 'Revenue',
                    data: [2500, 4200, 3800, 6500, 7200, 8500],
                    borderColor: '#667eea',
                    backgroundColor: 'rgba(102, 126, 234, 0.1)',
                    tension: 0.4,
                    fill: true,
                    pointBackgroundColor: '#667eea',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointRadius: 6,
                    pointHoverRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6b7280'
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: '#f3f4f6'
                        },
                        ticks: {
                            color: '#6b7280',
                            callback: function (value) {
                                return '$' + value.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    }

    // Category Pie Chart
    const categoryCtx = document.getElementById('categoryChart');
    if (categoryCtx && typeof Chart !== 'undefined') {
        new Chart(categoryCtx, {
            type: 'doughnut',
            data: {
                labels: ['Concert', 'Theatre', 'Cultural', 'Conference'],
                datasets: [{
                    data: [45, 25, 20, 10],
                    backgroundColor: [
                        '#667eea',
                        '#10b981',
                        '#f59e0b',
                        '#ef4444'
                    ],
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true
                        }
                    }
                }
            }
        });
    }

    // Weekly Sales Chart
    const weeklyCtx = document.getElementById('weeklyChart');
    if (weeklyCtx && typeof Chart !== 'undefined') {
        new Chart(weeklyCtx, {
            type: 'bar',
            data: {
                labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
                datasets: [{
                    label: 'Tickets Sold',
                    data: [45, 32, 58, 72, 95, 125, 89],
                    backgroundColor: 'rgba(102, 126, 234, 0.8)',
                    borderColor: '#667eea',
                    borderWidth: 1,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6b7280'
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: '#f3f4f6'
                        },
                        ticks: {
                            color: '#6b7280'
                        }
                    }
                }
            }
        });
    }
}

// Event form handling
document.addEventListener('DOMContentLoaded', function () {
    const eventForm = document.querySelector('.event-form');
    if (eventForm) {
        eventForm.addEventListener('submit', function (e) {
            e.preventDefault();

            // Collect form data
            const formData = new FormData(this);

            // Show success message (simulation)
            showNotification('Event created successfully!', 'success');

            // Reset form
            this.reset();

            // Navigate back to dashboard
            setTimeout(() => {
                // Activate dashboard
                menuLinks.forEach(l => l.classList.remove('active'));
                document.querySelector('.menu-link[data-page="dashboard"]').classList.add('active');

                // Show dashboard page
                pages.forEach(page => page.style.display = 'none');
                document.getElementById('dashboard-page').style.display = 'block';
            }, 2000);
        });
    }
});

// Notification system
function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <i class="fas ${getNotificationIcon(type)}"></i>
            <span>${message}</span>
        </div>
        <button class="notification-close">&times;</button>
    `;

    // Add styles if not already present
    if (!document.querySelector('.notification-styles')) {
        const styles = document.createElement('style');
        styles.className = 'notification-styles';
        styles.textContent = `
            .notification {
                position: fixed;
                top: 90px;
                right: 20px;
                background: white;
                border-radius: 8px;
                box-shadow: 0 4px 15px rgba(0,0,0,0.1);
                padding: 16px;
                display: flex;
                align-items: center;
                justify-content: space-between;
                z-index: 10000;
                min-width: 300px;
                border-left: 4px solid #667eea;
                animation: slideIn 0.3s ease;
            }
            .notification-success { border-left-color: #10b981; }
            .notification-warning { border-left-color: #f59e0b; }
            .notification-error { border-left-color: #ef4444; }
            .notification-content {
                display: flex;
                align-items: center;
                gap: 12px;
            }
            .notification-close {
                background: none;
                border: none;
                font-size: 18px;
                cursor: pointer;
                color: #6b7280;
            }
            @keyframes slideIn {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
        `;
        document.head.appendChild(styles);
    }

    document.body.appendChild(notification);

    // Auto remove after 5 seconds
    setTimeout(() => {
        notification.remove();
    }, 5000);

    // Manual close
    notification.querySelector('.notification-close').addEventListener('click', () => {
        notification.remove();
    });
}

function getNotificationIcon(type) {
    switch (type) {
        case 'success': return 'fa-check-circle';
        case 'warning': return 'fa-exclamation-triangle';
        case 'error': return 'fa-times-circle';
        default: return 'fa-info-circle';
    }
}

// Simulate real-time notifications
function simulateRealTimeNotifications() {
    const notifications = [
        { message: '3 new tickets sold for Summer Music Festival', type: 'success' },
        { message: 'Jazz Night event needs promotion - low sales', type: 'warning' },
        { message: 'New customer review received', type: 'info' },
        { message: 'Payment processed successfully', type: 'success' }
    ];

    let index = 0;
    setInterval(() => {
        if (index < notifications.length) {
            showNotification(notifications[index].message, notifications[index].type);
            index = (index + 1) % notifications.length;
        }
    }, 30000); // Show notification every 30 seconds
}

// Table action handlers
document.addEventListener('click', function (e) {
    if (e.target.closest('.btn-view')) {
        e.preventDefault();
        showNotification('Opening event details...', 'info');
    } else if (e.target.closest('.btn-edit')) {
        e.preventDefault();
        showNotification('Opening edit form...', 'info');
    } else if (e.target.closest('.btn-analytics')) {
        e.preventDefault();
        // Navigate to analytics page
        menuLinks.forEach(l => l.classList.remove('active'));
        document.querySelector('.menu-link[data-page="analytics"]').classList.add('active');
        pages.forEach(page => page.style.display = 'none');
        document.getElementById('analytics-page').style.display = 'block';
    }
});

// Search functionality
const searchInput = document.querySelector('.topbar-search input');
if (searchInput) {
    searchInput.addEventListener('input', function () {
        const searchTerm = this.value.toLowerCase();
        console.log('Searching for:', searchTerm);

        if (searchTerm.length > 2) {
            // Filter table rows based on search
            filterTableRows(searchTerm);
        } else {
            // Show all rows if search is cleared
            showAllTableRows();
        }
    });
}

function filterTableRows(searchTerm) {
    const tableRows = document.querySelectorAll('.custom-table tbody tr');
    tableRows.forEach(row => {
        const text = row.textContent.toLowerCase();
        if (text.includes(searchTerm)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function showAllTableRows() {
    const tableRows = document.querySelectorAll('.custom-table tbody tr');
    tableRows.forEach(row => {
        row.style.display = '';
    });
}

// Mobile responsiveness
function handleResize() {
    if (window.innerWidth <= 768) {
        sidebar.classList.add('collapsed');
        mainContent.classList.add('expanded');
    } else if (!sidebar.classList.contains('collapsed')) {
        sidebar.classList.remove('collapsed');
        mainContent.classList.remove('expanded');
    }
}

window.addEventListener('resize', handleResize);
handleResize(); // Initial check

// Mobile sidebar overlay for touch devices
if (window.innerWidth <= 768) {
    sidebarToggle.addEventListener('click', function (e) {
        e.stopPropagation();
        sidebar.classList.toggle('show');
    });

    document.addEventListener('click', function (e) {
        if (!sidebar.contains(e.target) && !sidebarToggle.contains(e.target)) {
            sidebar.classList.remove('show');
        }
    });
}

// Stats card hover effects
document.querySelectorAll('.stats-card').forEach(card => {
    card.addEventListener('mouseenter', function () {
        this.style.transform = 'translateY(-8px)';
    });

    card.addEventListener('mouseleave', function () {
        this.style.transform = 'translateY(0)';
    });
});

// Export functionality simulation
document.addEventListener('click', function (e) {
    if (e.target.textContent.includes('Export Reports')) {
        e.preventDefault();
        showNotification('Generating report... Download will start shortly.', 'info');

        setTimeout(() => {
            showNotification('Report exported successfully!', 'success');
        }, 3000);
    }
});

// User menu interaction
document.querySelector('.user-menu')?.addEventListener('click', function () {
    console.log('User menu clicked - dropdown functionality can be implemented here');
});

// Bell notification icon
document.querySelector('.fa-bell')?.addEventListener('click', function () {
    // Navigate to notifications page
    menuLinks.forEach(l => l.classList.remove('active'));
    const notificationLink = document.querySelector('.menu-link[data-page="notifications"]');
    if (notificationLink) {
        notificationLink.classList.add('active');
        pages.forEach(page => page.style.display = 'none');
        document.getElementById('notifications-page').style.display = 'block';
    }
});

// Auto-refresh dashboard data (simulation)
setInterval(() => {
    console.log('Auto-refreshing event organizer dashboard data...');
    // Here you would implement actual data refresh from server
    updateLiveStats();
}, 300000); // 5 minutes

function updateLiveStats() {
    // Simulate live stats updates
    const ticketsSold = document.querySelector('[data-target="1247"]');
    if (ticketsSold) {
        const currentValue = parseInt(ticketsSold.textContent.replace(/,/g, ''));
        const newValue = currentValue + Math.floor(Math.random() * 10);
        ticketsSold.textContent = newValue.toLocaleString();
    }
}

// Progress bar animations on scroll
function animateProgressBars() {
    const progressBars = document.querySelectorAll('.progress-bar');
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const progressBar = entry.target;
                const width = progressBar.style.width;
                progressBar.style.width = '0%';
                setTimeout(() => {
                    progressBar.style.width = width;
                }, 100);
            }
        });
    });

    progressBars.forEach(bar => observer.observe(bar));
}

// Initialize progress bar animations
document.addEventListener('DOMContentLoaded', function () {
    setTimeout(animateProgressBars, 1000);
});

// Form validation for event creation
function validateEventForm() {
    const form = document.querySelector('.event-form');
    if (!form) return;

    const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');

    inputs.forEach(input => {
        input.addEventListener('blur', function () {
            validateField(this);
        });

        input.addEventListener('input', function () {
            clearFieldError(this);
        });
    });
}

function validateField(field) {
    const value = field.value.trim();
    const fieldName = field.previousElementSibling?.textContent || 'This field';

    // Remove existing error
    clearFieldError(field);

    if (!value && field.hasAttribute('required')) {
        showFieldError(field, `${fieldName} is required`);
        return false;
    }

    // Additional validations
    if (field.type === 'email' && value && !isValidEmail(value)) {
        showFieldError(field, 'Please enter a valid email address');
        return false;
    }

    if (field.type === 'datetime-local' && value) {
        const selectedDate = new Date(value);
        const now = new Date();
        if (selectedDate <= now) {
            showFieldError(field, 'Event date must be in the future');
            return false;
        }
    }

    return true;
}

function showFieldError(field, message) {
    field.classList.add('error');
    const errorDiv = document.createElement('div');
    errorDiv.className = 'field-error';
    errorDiv.textContent = message;
    errorDiv.style.color = 'var(--danger)';
    errorDiv.style.fontSize = '12px';
    errorDiv.style.marginTop = '4px';
    field.parentNode.appendChild(errorDiv);
}

function clearFieldError(field) {
    field.classList.remove('error');
    const errorDiv = field.parentNode.querySelector('.field-error');
    if (errorDiv) {
        errorDiv.remove();
    }
}

function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

// Initialize form validation
document.addEventListener('DOMContentLoaded', validateEventForm);

// Dynamic table updates (simulation)
function updateEventTable() {
    const tableBody = document.querySelector('.custom-table tbody');
    if (!tableBody) return;

    // Simulate real-time updates to ticket sales
    const progressBars = tableBody.querySelectorAll('.progress-bar');
    const revenueElements = tableBody.querySelectorAll('td:nth-child(5) .fw-bold');

    progressBars.forEach((bar, index) => {
        const currentWidth = parseFloat(bar.style.width);
        const increment = Math.random() * 2; // 0-2% increase
        const newWidth = Math.min(currentWidth + increment, 100);

        bar.style.width = newWidth + '%';

        // Update corresponding revenue
        if (revenueElements[index]) {
            const currentRevenue = parseInt(revenueElements[index].textContent.replace(/[$,]/g, ''));
            const newRevenue = Math.floor(currentRevenue * (1 + increment / 100));
            revenueElements[index].textContent = ' + newRevenue.toLocaleString();
        }
    });
}

// Update table data every 2 minutes (simulation)
setInterval(updateEventTable, 120000);

// Keyboard shortcuts
document.addEventListener('keydown', function (e) {
    // Ctrl/Cmd + N for new event
    if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
        e.preventDefault();
        // Navigate to create event page
        menuLinks.forEach(l => l.classList.remove('active'));
        document.querySelector('.menu-link[data-page="create-event"]')?.classList.add('active');
        pages.forEach(page => page.style.display = 'none');
        document.getElementById('create-event-page').style.display = 'block';
    }

    // Ctrl/Cmd + D for dashboard
    if ((e.ctrlKey || e.metaKey) && e.key === 'd') {
        e.preventDefault();
        menuLinks.forEach(l => l.classList.remove('active'));
        document.querySelector('.menu-link[data-page="dashboard"]')?.classList.add('active');
        pages.forEach(page => page.style.display = 'none');
        document.getElementById('dashboard-page').style.display = 'block';
    }

    // ESC to close notifications
    if (e.key === 'Escape') {
        document.querySelectorAll('.notification').forEach(notification => {
            notification.remove();
        });
    }
});

// Touch gestures for mobile
let startX = 0;
let startY = 0;

document.addEventListener('touchstart', function (e) {
    startX = e.touches[0].clientX;
    startY = e.touches[0].clientY;
});

document.addEventListener('touchend', function (e) {
    if (!startX || !startY) return;

    const endX = e.changedTouches[0].clientX;
    const endY = e.changedTouches[0].clientY;

    const deltaX = startX - endX;
    const deltaY = startY - endY;

    // Swipe right to open sidebar on mobile
    if (Math.abs(deltaX) > Math.abs(deltaY) && deltaX < -100 && window.innerWidth <= 768) {
        sidebar.classList.add('show');
    }

    // Swipe left to close sidebar on mobile
    if (Math.abs(deltaX) > Math.abs(deltaY) && deltaX > 100 && window.innerWidth <= 768) {
        sidebar.classList.remove('show');
    }

    startX = 0;
    startY = 0;
});

// Local storage for user preferences (if available)
function saveUserPreference(key, value) {
    try {
        localStorage.setItem(`eventOrganizer_${key}`, JSON.stringify(value));
    } catch (e) {
        console.log('Local storage not available');
    }
}

function getUserPreference(key, defaultValue) {
    try {
        const stored = localStorage.getItem(`eventOrganizer_${key}`);
        return stored ? JSON.parse(stored) : defaultValue;
    } catch (e) {
        return defaultValue;
    }
}

// Remember sidebar state
sidebarToggle.addEventListener('click', function () {
    const isCollapsed = sidebar.classList.contains('collapsed');
    saveUserPreference('sidebarCollapsed', isCollapsed);
});

// Restore sidebar state on load
document.addEventListener('DOMContentLoaded', function () {
    const isCollapsed = getUserPreference('sidebarCollapsed', false);
    if (isCollapsed) {
        sidebar.classList.add('collapsed');
        mainContent.classList.add('expanded');
    }
});

// Performance monitoring
function logPerformance() {
    if ('performance' in window) {
        const perfData = performance.getEntriesByType('navigation')[0];
        console.log('Page load time:', perfData.loadEventEnd - perfData.loadEventStart + 'ms');
    }
}

// Log performance after page load
window.addEventListener('load', logPerformance);

// Error handling for charts
window.addEventListener('error', function (e) {
    if (e.message.includes('Chart')) {
        console.warn('Chart.js not loaded. Charts will not be displayed.');
        // Hide chart containers if Chart.js is not available
        document.querySelectorAll('.chart-container').forEach(container => {
            container.innerHTML = '<p style="text-align: center; color: #6b7280; padding: 40px;">Chart.js library required for data visualization</p>';
        });
    }
});

// Print functionality
function printReport() {
    window.print();
}

// Add print button functionality if exists
document.addEventListener('click', function (e) {
    if (e.target.textContent?.includes('Print') || e.target.closest('[data-action="print"]')) {
        e.preventDefault();
        printReport();
    }
});

// Initialize tooltips for action buttons
function initializeTooltips() {
    const actionButtons = document.querySelectorAll('.btn-action[title]');
    actionButtons.forEach(button => {
        button.addEventListener('mouseenter', function (e) {
            showTooltip(e.target, e.target.getAttribute('title'));
        });

        button.addEventListener('mouseleave', function () {
            hideTooltip();
        });
    });
}

function showTooltip(element, text) {
    const tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    tooltip.textContent = text;
    tooltip.style.cssText = `
        position: absolute;
        background: #333;
        color: white;
        padding: 6px 10px;
        border-radius: 4px;
        font-size: 12px;
        white-space: nowrap;
        z-index: 10000;
        pointer-events: none;
    `;

    document.body.appendChild(tooltip);

    const rect = element.getBoundingClientRect();
    tooltip.style.left = rect.left + rect.width / 2 - tooltip.offsetWidth / 2 + 'px';
    tooltip.style.top = rect.top - tooltip.offsetHeight - 8 + 'px';
}

function hideTooltip() {
    const tooltip = document.querySelector('.tooltip');
    if (tooltip) tooltip.remove();
}

// Initialize tooltips
document.addEventListener('DOMContentLoaded', initializeTooltips);

console.log('Event Organizer Dashboard initialized successfully');