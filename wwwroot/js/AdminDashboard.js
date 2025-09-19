// Sidebar functionality
const sidebar = document.getElementById('sidebar');
const mainContent = document.getElementById('mainContent');
const sidebarToggle = document.getElementById('sidebarToggle');

sidebarToggle.addEventListener('click', function () {
    sidebar.classList.toggle('collapsed');
    mainContent.classList.toggle('expanded');
});

// Navigation functionality
const menuLinks = document.querySelectorAll('.menu-link');
const pages = document.querySelectorAll('.page-content');

menuLinks.forEach(link => {
    link.addEventListener('click', function (e) {
        // if it's an external link, allow redirect
        if (this.classList.contains("external-link")) {
            return; // do not prevent default
        }

        e.preventDefault(); // block only internal (SPA-style) links

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


// Counter animation
function animateCounter(element) {
    const target = parseFloat(element.dataset.target.replace(/[,$]/g, ''));
    const increment = target / 100;
    let current = 0;

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            current = target;
            clearInterval(timer);
        }

        if (element.dataset.target.includes('$')) {
            element.textContent = '$' + Math.floor(current).toLocaleString();
        } else {
            element.textContent = Math.floor(current).toLocaleString();
        }
    }, 20);
}

// Initialize counter animations
document.addEventListener('DOMContentLoaded', function () {
    setTimeout(() => {
        const counters = document.querySelectorAll('.stats-value[data-target]');
        counters.forEach(counter => animateCounter(counter));
    }, 500);

    // Initialize revenue chart
    const ctx = document.getElementById('revenueChart');
    if (ctx && window.Chart) {
        const labels = (() => { try { return JSON.parse(ctx.getAttribute('data-labels')) } catch { return []; } })();
        const values = (() => { try { return JSON.parse(ctx.getAttribute('data-values')) } catch { return []; } })();
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Revenue',
                    data: values,
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
                },
                interaction: {
                    intersect: false,
                    mode: 'index'
                }
            }
        });
    }
});

// Mobile responsiveness
function handleResize() {
    if (window.innerWidth <= 768) {
        sidebar.classList.add('collapsed');
        mainContent.classList.add('expanded');
    } else {
        sidebar.classList.remove('collapsed');
        mainContent.classList.remove('expanded');
    }
}

window.addEventListener('resize', handleResize);
handleResize(); // Initial check

// Mobile sidebar overlay
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

// Smooth transitions for stats cards
document.querySelectorAll('.stats-card').forEach(card => {
    card.addEventListener('mouseenter', function () {
        this.style.transform = 'translateY(-8px)';
    });

    card.addEventListener('mouseleave', function () {
        this.style.transform = 'translateY(0)';
    });
});

// Auto-refresh functionality (placeholder)
setInterval(() => {
    console.log('Auto-refreshing dashboard data...');
    // Here you would implement actual data refresh logic
}, 300000); // 5 minutes

// Search functionality (placeholder)
const searchInput = document.querySelector('.topbar-search input');
searchInput.addEventListener('input', function () {
    const searchTerm = this.value.toLowerCase();
    console.log('Searching for:', searchTerm);
    // Implement actual search logic here
});

// Notification click handler
document.querySelector('.fa-bell').addEventListener('click', function () {
    console.log('Showing notifications...');
    // Implement notification dropdown here
});

// User menu click handler
document.querySelector('.user-menu').addEventListener('click', function () {
    console.log('Showing user menu...');
    // Implement user menu dropdown here
});