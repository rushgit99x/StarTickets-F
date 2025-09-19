document.addEventListener('DOMContentLoaded', function () {
    // Initialize all functionality
    initHeroSlider();
    initNavigation();
    initEventTabs();
    initAnimations();
    initSearch();
    initNewsletter();
    initBookingButtons();
    initCategoryFilters();
});

// Global variables
let currentEvents = [];
let isLoading = false;

// Hero Slider Functionality (Enhanced)
function initHeroSlider() {
    const slides = document.querySelectorAll('.hero-slide');
    const dots = document.querySelectorAll('.dot');
    const prevBtn = document.querySelector('.hero-prev');
    const nextBtn = document.querySelector('.hero-next');
    let currentSlide = 0;
    let slideInterval;

    function showSlide(index) {
        slides.forEach(slide => slide.classList.remove('active'));
        dots.forEach(dot => dot.classList.remove('active'));

        slides[index].classList.add('active');
        dots[index].classList.add('active');
        currentSlide = index;
    }

    function nextSlide() {
        currentSlide = (currentSlide + 1) % slides.length;
        showSlide(currentSlide);
    }

    function prevSlide() {
        currentSlide = (currentSlide - 1 + slides.length) % slides.length;
        showSlide(currentSlide);
    }

    function startSlideShow() {
        slideInterval = setInterval(nextSlide, 5000);
    }

    function stopSlideShow() {
        clearInterval(slideInterval);
    }

    // Event listeners
    if (nextBtn) nextBtn.addEventListener('click', () => {
        stopSlideShow();
        nextSlide();
        startSlideShow();
    });

    if (prevBtn) prevBtn.addEventListener('click', () => {
        stopSlideShow();
        prevSlide();
        startSlideShow();
    });

    dots.forEach((dot, index) => {
        dot.addEventListener('click', () => {
            stopSlideShow();
            showSlide(index);
            startSlideShow();
        });
    });

    const heroSection = document.querySelector('.hero');
    if (heroSection) {
        heroSection.addEventListener('mouseenter', stopSlideShow);
        heroSection.addEventListener('mouseleave', startSlideShow);
    }

    startSlideShow();
}

// Enhanced Navigation
function initNavigation() {
    const hamburger = document.querySelector('.hamburger');
    const navMenu = document.querySelector('.nav-menu');
    const navLinks = document.querySelectorAll('.nav-link');

    if (hamburger) {
        hamburger.addEventListener('click', () => {
            hamburger.classList.toggle('active');
            navMenu.classList.toggle('active');
        });
    }

    navLinks.forEach(link => {
        link.addEventListener('click', () => {
            hamburger?.classList.remove('active');
            navMenu?.classList.remove('active');
        });
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });

    // Navbar background change on scroll
    window.addEventListener('scroll', () => {
        const header = document.querySelector('.header');
        if (window.scrollY > 100) {
            header?.classList.add('scrolled');
        } else {
            header?.classList.remove('scrolled');
        }
    });
}

// Enhanced Search Functionality
function initSearch() {
    // Hero search
    const heroSearchBtn = document.getElementById('hero-search-btn');
    const heroSearchQuery = document.getElementById('hero-search-query');

    // Navigation search
    const navSearchBtn = document.getElementById('nav-search-btn');
    const navSearchInput = document.getElementById('nav-search-input');

    if (heroSearchBtn) {
        heroSearchBtn.addEventListener('click', performHeroSearch);
    }

    if (navSearchBtn) {
        navSearchBtn.addEventListener('click', performNavSearch);
    }

    if (heroSearchQuery) {
        heroSearchQuery.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                performHeroSearch();
            }
        });
    }

    if (navSearchInput) {
        navSearchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                performNavSearch();
            }
        });
    }
}

async function performHeroSearch() {
    const query = document.getElementById('hero-search-query')?.value || '';
    const location = document.getElementById('hero-search-location')?.value || '';
    const categoryId = document.getElementById('hero-search-category')?.value || '';
    const date = document.getElementById('hero-search-date')?.value || '';

    await searchEvents(query, categoryId, location, date);
}

async function performNavSearch() {
    const query = document.getElementById('nav-search-input')?.value || '';
    
    console.log('Navigation search query:', query);
    
    // Check if the query matches any venue names or cities
    const venueMatch = findVenueMatch(query);
    
    if (venueMatch) {
        console.log('Venue match found:', venueMatch);
        // If it's a venue match, search with location parameter only (no query)
        await searchEvents('', '', venueMatch, '');
    } else {
        console.log('No venue match, performing regular search');
        // Regular search
        await searchEvents(query);
    }
}

function findVenueMatch(query) {
    // Get venue data from server
    if (window.serverData && window.serverData.venues) {
        const venues = window.serverData.venues;
        console.log('Available venues:', venues);
        
        // Check if query matches any venue name or city
        const venueMatch = venues.find(venue => 
            venue.name.toLowerCase().includes(query.toLowerCase()) ||
            venue.city.toLowerCase().includes(query.toLowerCase())
        );
        
        if (venueMatch) {
            console.log('Found venue match:', venueMatch);
            // If it's a venue name match, return the venue name
            // If it's a city match, return the city
            if (venueMatch.name.toLowerCase().includes(query.toLowerCase())) {
                return venueMatch.name;
            } else {
                return venueMatch.city;
            }
        }
    } else {
        console.log('No venue data available in window.serverData');
    }
    
    return null;
}

async function searchEvents(query = '', categoryId = '', location = '', date = '') {
    if (isLoading) return;

    console.log('Search parameters:', { query, categoryId, location, date });

    showLoadingSpinner();
    isLoading = true;

    try {
        const params = new URLSearchParams();
        if (query) params.append('query', query);
        if (categoryId) params.append('categoryId', categoryId);
        if (location) params.append('location', location);
        if (date) params.append('date', date);

        console.log('Search URL:', `${window.serverData.searchUrl}?${params}`);

        const response = await fetch(`${window.serverData.searchUrl}?${params}`);
        const data = await response.json();

        console.log('Search response:', data);

        if (data.success) {
            displaySearchResults(data.events, query);
            showNotification(`Found ${data.events.length} events`, 'success');
        } else {
            showNotification('Search failed. Please try again.', 'error');
        }
    } catch (error) {
        console.error('Search error:', error);
        showNotification('Search failed. Please check your connection.', 'error');
    } finally {
        hideLoadingSpinner();
        isLoading = false;
    }
}

function displaySearchResults(events, query) {
    const searchSection = document.getElementById('search-results');
    const searchTitle = document.getElementById('search-results-title');
    const searchGrid = document.getElementById('search-results-grid');

    if (!searchSection || !searchGrid) return;

    // Update title
    if (searchTitle) {
        searchTitle.textContent = query ?
            `Search Results for "${query}" (${events.length} events)` :
            `Search Results (${events.length} events)`;
    }

    // Clear existing results
    searchGrid.innerHTML = '';

    if (events.length === 0) {
        searchGrid.innerHTML = `
            <div class="no-events">
                <i class="fas fa-search"></i>
                <h3>No events found</h3>
                <p>Try adjusting your search criteria</p>
            </div>
        `;
    } else {
        // Create event cards
        events.forEach(event => {
            const eventCard = createEventCard(event);
            searchGrid.appendChild(eventCard);
        });
    }

    // Show search results section
    searchSection.style.display = 'block';
    searchSection.scrollIntoView({ behavior: 'smooth' });
}

function createEventCard(event) {
    const card = document.createElement('div');
    card.className = 'event-card';
    card.setAttribute('data-event-id', event.id);

    card.innerHTML = `
        <div class="event-image">
            <img src="${event.image || 'https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=400&h=300&fit=crop'}" 
                 alt="${event.name}" onerror="this.src='https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=400&h=300&fit=crop'">
            <div class="event-date">
                <span class="day">${new Date(event.date).getDate()}</span>
                <span class="month">${new Date(event.date).toLocaleString('en-US', { month: 'short' }).toUpperCase()}</span>
            </div>
            <div class="event-category">${event.category || ''}</div>
        </div>
        <div class="event-info">
            <h3>${event.name}</h3>
            <p class="event-venue">
                <i class="fas fa-map-marker-alt"></i> ${event.venue}${event.city ? ', ' + event.city : ''}
            </p>
            <p class="event-time">
                <i class="fas fa-clock"></i> ${event.time}
            </p>
            <div class="event-price">
                <span class="price">From ${event.minPrice}</span>
                <button class="btn btn-primary book-now-btn" 
                        data-event-id="${event.id}" 
                        data-event-name="${event.name}">
                    Book Now
                </button>
            </div>
        </div>
    `;

    return card;
}

// Category Filter Functionality
function initCategoryFilters() {
    // Category cards
    const categoryCards = document.querySelectorAll('.category-card');
    categoryCards.forEach(card => {
        card.addEventListener('click', () => {
            const categoryId = card.getAttribute('data-category-id');
            const categoryName = card.querySelector('h3').textContent;
            filterByCategory(categoryId, categoryName);
        });
    });

    // Dropdown category links
    const categoryLinks = document.querySelectorAll('[data-category]');
    categoryLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const categoryId = link.getAttribute('data-category');
            const categoryName = link.textContent.trim();
            filterByCategory(categoryId, categoryName);
        });
    });
}

async function filterByCategory(categoryId, categoryName) {
    if (isLoading) return;

    showLoadingSpinner();
    isLoading = true;

    try {
        const response = await fetch(`${window.serverData.categoryUrl}?categoryId=${categoryId}`);
        const data = await response.json();

        if (data.success) {
            displaySearchResults(data.events, '');
            const searchTitle = document.getElementById('search-results-title');
            if (searchTitle) {
                searchTitle.textContent = `${categoryName} Events (${data.events.length} events)`;
            }
            showNotification(`Found ${data.events.length} ${categoryName.toLowerCase()} events`, 'success');
        } else {
            showNotification('Failed to load category events.', 'error');
        }
    } catch (error) {
        console.error('Category filter error:', error);
        showNotification('Failed to load events. Please try again.', 'error');
    } finally {
        hideLoadingSpinner();
        isLoading = false;
    }
}

// Booking Functionality
function initBookingButtons() {
    document.addEventListener('click', (e) => {
        if (e.target.classList.contains('book-now-btn')) {
            e.preventDefault();
            const eventId = e.target.getAttribute('data-event-id');
            const eventName = e.target.getAttribute('data-event-name');
            handleBooking(eventId, eventName);
        }
    });
}

function handleBooking(eventId, eventName) {
    if (!window.serverData.isAuthenticated) {
        showNotification('Please login to book tickets', 'info');
        setTimeout(() => {
            window.location.href = window.serverData.bookingUrl;
        }, 1500);
        return;
    }

    // If authenticated, redirect to booking page
    showNotification(`Redirecting to booking for ${eventName}...`, 'success');
    setTimeout(() => {
        window.location.href = `${window.serverData.bookingUrl}?eventId=${eventId}`;
    }, 1000);
}

// Enhanced Newsletter Functionality
function initNewsletter() {
    const newsletterForm = document.getElementById('newsletter-form');
    const emailInput = document.getElementById('newsletter-email');

    if (newsletterForm) {
        newsletterForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            const email = emailInput.value.trim();

            if (!validateEmail(email)) {
                showNotification('Please enter a valid email address.', 'error');
                return;
            }

            try {
                const response = await fetch(window.serverData.subscribeUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                    },
                    body: `email=${encodeURIComponent(email)}`
                });

                const data = await response.json();

                if (data.success) {
                    showNotification(data.message || 'Successfully subscribed!', 'success');
                    emailInput.value = '';
                } else {
                    showNotification(data.message || 'Subscription failed.', 'error');
                }
            } catch (error) {
                console.error('Newsletter subscription error:', error);
                showNotification('Subscription failed. Please try again.', 'error');
            }
        });
    }
}

// Event Tabs Functionality
function initEventTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');

    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            // Remove active class from all buttons
            tabButtons.forEach(btn => btn.classList.remove('active'));

            // Add active class to clicked button
            button.classList.add('active');

            const tabType = button.dataset.tab;
            filterEventsByTab(tabType);
        });
    });

    // Upcoming tabs (This Week, This Month, Next Month)
    const upcomingTabButtons = document.querySelectorAll('.upcoming-tab-btn');
    const weekGrid = document.getElementById('upcoming-week-grid');
    const thisMonthGrid = document.getElementById('upcoming-thismonth-grid');
    const nextMonthGrid = document.getElementById('upcoming-nextmonth-grid');

    function showUpcomingGrid(name) {
        if (weekGrid) weekGrid.style.display = name === 'week' ? 'grid' : 'none';
        if (thisMonthGrid) thisMonthGrid.style.display = name === 'this-month' ? 'grid' : 'none';
        if (nextMonthGrid) nextMonthGrid.style.display = name === 'next-month' ? 'grid' : 'none';
    }

    upcomingTabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            upcomingTabButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const which = btn.getAttribute('data-upcoming-tab');
            showUpcomingGrid(which);
            showNotification(`Loading ${btn.textContent} events...`, 'info');
        });
    });

    // Ensure default visible grid
    showUpcomingGrid('week');
}

async function filterEventsByTab(tabType) {
    // This could be enhanced to call different API endpoints
    // For now, we'll just show a loading animation
    const eventsGrid = document.getElementById('events-grid');
    const eventCards = eventsGrid.querySelectorAll('.event-card');

    // Add loading animation
    eventCards.forEach((card, index) => {
        card.style.animation = 'none';
        card.offsetHeight; // Trigger reflow
        card.style.animation = `fadeIn 0.6s ease ${index * 0.1}s both`;
    });

    showNotification(`Loading ${tabType} events...`, 'info');
}

// Animation Functionality
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
        '.category-card, .event-card, .promo-banner'
    );

    animateElements.forEach(el => {
        observer.observe(el);
    });

    // Parallax effect for hero section
    window.addEventListener('scroll', () => {
        const scrolled = window.pageYOffset;
        const parallax = document.querySelector('.hero-slide.active');

        if (parallax && scrolled < window.innerHeight) {
            const speed = scrolled * 0.3;
            parallax.style.transform = `translateY(${speed}px)`;
        }
    });
}

// Utility Functions
function showLoadingSpinner() {
    const spinner = document.getElementById('loading-spinner');
    if (spinner) {
        spinner.style.display = 'flex';
    }
}

function hideLoadingSpinner() {
    const spinner = document.getElementById('loading-spinner');
    if (spinner) {
        spinner.style.display = 'none';
    }
}

function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function showNotification(message, type = 'info') {
    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => {
        notification.remove();
    });

    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;

    // Add icon based on type
    const icon = type === 'success' ? 'check-circle' :
        type === 'error' ? 'exclamation-circle' :
            type === 'info' ? 'info-circle' : 'bell';

    notification.innerHTML = `
        <i class="fas fa-${icon}"></i>
        <span>${message}</span>
    `;

    // Add notification styles if not already present
    if (!document.querySelector('#notification-styles')) {
        const styleSheet = document.createElement('style');
        styleSheet.id = 'notification-styles';
        styleSheet.textContent = `
            .notification {
                position: fixed;
                top: 100px;
                right: 20px;
                padding: 15px 20px;
                border-radius: 10px;
                color: white;
                font-weight: 500;
                z-index: 10001;
                animation: slideInRight 0.3s ease, fadeOut 0.3s ease 4.7s;
                max-width: 350px;
                word-wrap: break-word;
                display: flex;
                align-items: center;
                gap: 10px;
                box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
            }
            .notification-success {
                background: linear-gradient(135deg, #10b981, #059669);
            }
            .notification-error {
                background: linear-gradient(135deg, #ef4444, #dc2626);
            }
            .notification-info {
                background: linear-gradient(135deg, #3b82f6, #2563eb);
            }
            .notification i {
                font-size: 18px;
                flex-shrink: 0;
            }
            @keyframes slideInRight {
                from {
                    transform: translateX(100%);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }
            @keyframes fadeOut {
                from {
                    opacity: 1;
                }
                to {
                    opacity: 0;
                }
            }
            
            /* Loading Spinner Styles */
            .loading-spinner {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.7);
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                z-index: 10000;
                color: white;
            }
            
            .spinner {
                width: 50px;
                height: 50px;
                border: 3px solid rgba(255, 255, 255, 0.3);
                border-top: 3px solid #fff;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                margin-bottom: 15px;
            }
            
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
            
            /* No Events Styles */
            .no-events {
                text-align: center;
                padding: 60px 20px;
                color: #6b7280;
                grid-column: 1 / -1;
            }
            
            .no-events i {
                font-size: 48px;
                margin-bottom: 20px;
                opacity: 0.6;
            }
            
            .no-events h3 {
                font-size: 24px;
                margin-bottom: 10px;
                color: #374151;
            }
        `;
        document.head.appendChild(styleSheet);
    }

    document.body.appendChild(notification);

    // Remove after 5 seconds
    setTimeout(() => {
        if (document.body.contains(notification)) {
            notification.style.animation = 'fadeOut 0.3s ease forwards';
            setTimeout(() => {
                if (document.body.contains(notification)) {
                    document.body.removeChild(notification);
                }
            }, 300);
        }
    }, 5000);
}

// Enhanced error handling for images
document.addEventListener('error', (e) => {
    if (e.target.tagName === 'IMG') {
        e.target.src = 'https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=400&h=300&fit=crop';
        e.target.alt = 'Event image';
    }
}, true);

// Keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Escape key to close search results
    if (e.key === 'Escape') {
        const searchSection = document.getElementById('search-results');
        if (searchSection && searchSection.style.display !== 'none') {
            searchSection.style.display = 'none';
        }
    }

    // Ctrl/Cmd + K to focus search
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.getElementById('nav-search-input');
        if (searchInput) {
            searchInput.focus();
        }
    }
});

// Window load event
window.addEventListener('load', () => {
    document.body.classList.add('loaded');
    hideLoadingSpinner();

    // Check for search parameters in URL
    const urlParams = new URLSearchParams(window.location.search);
    const searchQuery = urlParams.get('search');
    if (searchQuery) {
        const navSearchInput = document.getElementById('nav-search-input');
        if (navSearchInput) {
            navSearchInput.value = searchQuery;
            performNavSearch();
        }
    }
});

// Enhanced performance monitoring
let performanceMetrics = {
    searchTime: 0,
    loadTime: 0
};

function trackPerformance(action, startTime) {
    const endTime = performance.now();
    const duration = endTime - startTime;
    performanceMetrics[action] = duration;
    console.log(`${action} took ${duration.toFixed(2)}ms`);
}

// Export for external use
window.StarTickets = {
    searchEvents,
    filterByCategory,
    showNotification,
    handleBooking
};