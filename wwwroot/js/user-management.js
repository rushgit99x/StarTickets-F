$(document).ready(function () {
    initializeUserManagement();
});

function initializeUserManagement() {
    // Initialize tooltips
    initTooltips();

    // Initialize search functionality
    initSearchFunctionality();

    // Initialize bulk selection
    initBulkSelection();

    // Initialize filters
    initFilters();

    // Initialize responsive table
    initResponsiveTable();

    // Initialize keyboard shortcuts
    initKeyboardShortcuts();
}

// Initialize tooltips for action buttons
function initTooltips() {
    $('[title]').tooltip({
        placement: 'top',
        trigger: 'hover'
    });
}

// Initialize search functionality with debounce
function initSearchFunctionality() {
    const searchInput = $('.search-input');
    let searchTimeout;

    searchInput.on('input', function () {
        clearTimeout(searchTimeout);
        const query = $(this).val();

        searchTimeout = setTimeout(() => {
            if (query.length >= 3 || query.length === 0) {
                performSearch(query);
            }
        }, 300);
    });

    // Handle search form submission
    $('.filters-form').on('submit', function (e) {
        e.preventDefault();
        const formData = $(this).serialize();
        window.location.href = '?' + formData;
    });
}

function performSearch(query) {
    // Add loading indicator
    const searchBtn = $('.search-btn');
    const originalIcon = searchBtn.html();
    searchBtn.html('<i class="fas fa-spinner fa-spin"></i>');

    // Simulate search delay (in real implementation, this would be an AJAX call)
    setTimeout(() => {
        searchBtn.html(originalIcon);
        // In a real implementation, update the table with filtered results
    }, 500);
}

// Initialize bulk selection functionality
function initBulkSelection() {
    // Handle select all checkboxes
    $('#selectAll, #selectAllTable').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('.user-checkbox').prop('checked', isChecked);
        updateBulkActionsVisibility();
        updateSelectAllState();
    });

    // Handle individual checkboxes
    $(document).on('change', '.user-checkbox', function () {
        updateBulkActionsVisibility();
        updateSelectAllState();
    });
}

function updateBulkActionsVisibility() {
    const selectedCount = $('.user-checkbox:checked').length;
    const bulkButtons = $('.bulk-buttons');

    if (selectedCount > 0) {
        bulkButtons.slideDown(200);
        updateBulkButtonText(selectedCount);
    } else {
        bulkButtons.slideUp(200);
    }
}

function updateBulkButtonText(count) {
    $('.bulk-buttons button').each(function () {
        const originalText = $(this).data('original-text') || $(this).text();
        if (!$(this).data('original-text')) {
            $(this).data('original-text', originalText);
        }
        $(this).text(originalText + ' (' + count + ')');
    });
}

function updateSelectAllState() {
    const totalCheckboxes = $('.user-checkbox').length;
    const checkedCheckboxes = $('.user-checkbox:checked').length;
    const selectAllCheckboxes = $('#selectAll, #selectAllTable');

    if (checkedCheckboxes === 0) {
        selectAllCheckboxes.prop('indeterminate', false);
        selectAllCheckboxes.prop('checked', false);
    } else if (checkedCheckboxes === totalCheckboxes) {
        selectAllCheckboxes.prop('indeterminate', false);
        selectAllCheckboxes.prop('checked', true);
    } else {
        selectAllCheckboxes.prop('indeterminate', true);
    }
}

// Initialize filter functionality
function initFilters() {
    // Auto-submit filters on change
    $('.filter-select').on('change', function () {
        $(this).closest('form').submit();
    });

    // Clear filters functionality
    $('.btn:contains("Clear")').on('click', function (e) {
        e.preventDefault();
        window.location.href = window.location.pathname;
    });
}

// Initialize responsive table functionality
function initResponsiveTable() {
    // Add responsive wrapper if not exists
    if (!$('.table-container').parent().hasClass('table-responsive-wrapper')) {
        $('.table-container').wrap('<div class="table-responsive-wrapper"></div>');
    }

    // Handle table scroll indicators
    const tableContainer = $('.table-container');
    const table = $('.users-table');

    function updateScrollIndicators() {
        const scrollLeft = tableContainer.scrollLeft();
        const scrollWidth = table[0].scrollWidth;
        const clientWidth = tableContainer[0].clientWidth;

        // Add classes for scroll indicators (you can style these)
        if (scrollLeft > 0) {
            tableContainer.addClass('scrolled-left');
        } else {
            tableContainer.removeClass('scrolled-left');
        }

        if (scrollLeft + clientWidth < scrollWidth) {
            tableContainer.addClass('can-scroll-right');
        } else {
            tableContainer.removeClass('can-scroll-right');
        }
    }

    tableContainer.on('scroll', updateScrollIndicators);
    updateScrollIndicators();
}

// Initialize keyboard shortcuts
function initKeyboardShortcuts() {
    $(document).on('keydown', function (e) {
        // Ctrl/Cmd + A to select all
        if ((e.ctrlKey || e.metaKey) && e.key === 'a' && !$(e.target).is('input, textarea')) {
            e.preventDefault();
            $('#selectAll').click();
        }

        // Escape to clear selection
        if (e.key === 'Escape') {
            $('.user-checkbox, #selectAll, #selectAllTable').prop('checked', false);
            updateBulkActionsVisibility();
        }

        // Ctrl/Cmd + F to focus search
        if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
            e.preventDefault();
            $('.search-input').focus();
        }
    });
}

// Bulk action functions
function bulkAction(action) {
    const selectedUsers = $('.user-checkbox:checked').map(function () {
        return parseInt($(this).val());
    }).get();

    if (selectedUsers.length === 0) {
        showNotification('Please select at least one user.', 'warning');
        return;
    }

    const actionText = getActionText(action);
    const confirmMessage = `Are you sure you want to ${actionText} ${selectedUsers.length} user(s)?`;

    showConfirmDialog(confirmMessage, function () {
        performBulkAction(action, selectedUsers);
    });
}

function getActionText(action) {
    switch (action) {
        case 'activate': return 'activate';
        case 'deactivate': return 'deactivate';
        case 'confirm_email': return 'confirm email for';
        default: return 'perform action on';
    }
}

function performBulkAction(action, userIds) {
    // Show loading state
    showLoadingOverlay();

    $.post('/UserManagement/BulkAction', {
        action: action,
        userIds: userIds,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
        .done(function (response) {
            hideLoadingOverlay();
            if (response.success) {
                showNotification(response.message, 'success');
                // Clear selections
                $('.user-checkbox, #selectAll, #selectAllTable').prop('checked', false);
                updateBulkActionsVisibility();
                // Reload page after short delay
                setTimeout(() => location.reload(), 1000);
            } else {
                showNotification(response.message, 'error');
            }
        })
        .fail(function (xhr) {
            hideLoadingOverlay();
            const errorMsg = xhr.responseJSON?.message || 'An error occurred while performing bulk action.';
            showNotification(errorMsg, 'error');
        });
}

// User status toggle
function toggleUserStatus(userId, currentStatus) {
    const action = currentStatus ? 'deactivate' : 'activate';
    const userName = $(`tr[data-user-id="${userId}"] .user-name`).text();

    const confirmMessage = `Are you sure you want to ${action} ${userName}?`;

    showConfirmDialog(confirmMessage, function () {
        performStatusToggle(userId);
    });
}

function performStatusToggle(userId) {
    const row = $(`tr[data-user-id="${userId}"]`);
    const statusBadge = row.find('.status-badge');

    // Show loading state on the row
    row.addClass('updating');

    $.post('/UserManagement/ToggleStatus', {
        id: userId,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
        .done(function (response) {
            row.removeClass('updating');
            if (response.success) {
                showNotification(response.message, 'success');

                // Update the status badge and button
                if (response.isActive) {
                    statusBadge.removeClass('status-inactive').addClass('status-active').text('Active');
                    row.find('.btn-success').removeClass('btn-success').addClass('btn-warning')
                        .attr('title', 'Deactivate User')
                        .html('<i class="fas fa-ban"></i>');
                } else {
                    statusBadge.removeClass('status-active').addClass('status-inactive').text('Inactive');
                    row.find('.btn-warning').removeClass('btn-warning').addClass('btn-success')
                        .attr('title', 'Activate User')
                        .html('<i class="fas fa-check"></i>');
                }

                // Update the onclick attribute
                const button = row.find('.btn-warning, .btn-success');
                button.attr('onclick', `toggleUserStatus(${userId}, ${response.isActive})`);

            } else {
                showNotification(response.message, 'error');
            }
        })
        .fail(function (xhr) {
            row.removeClass('updating');
            const errorMsg = xhr.responseJSON?.message || 'An error occurred while updating user status.';
            showNotification(errorMsg, 'error');
        });
}

// Password reset functionality
let currentResetUserId = null;

function showResetPasswordModal(userId, userName) {
    currentResetUserId = userId;
    $('#resetUserName').text(userName);
    $('#newPassword').val('');

    // Clear previous validation
    $('#newPassword').removeClass('is-invalid');
    $('.invalid-feedback').remove();

    const modal = new bootstrap.Modal(document.getElementById('resetPasswordModal'));
    modal.show();
}

function resetPassword() {
    const newPassword = $('#newPassword').val();
    const passwordField = $('#newPassword');

    // Clear previous validation
    passwordField.removeClass('is-invalid');
    $('.invalid-feedback').remove();

    if (!newPassword || newPassword.length < 6) {
        passwordField.addClass('is-invalid');
        passwordField.after('<div class="invalid-feedback">Password must be at least 6 characters long.</div>');
        return;
    }

    // Show loading state
    const submitBtn = $('#resetPasswordModal .btn-primary');
    const originalText = submitBtn.text();
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Resetting...');

    $.post('/UserManagement/ResetPassword', {
        id: currentResetUserId,
        newPassword: newPassword,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
        .done(function (response) {
            submitBtn.prop('disabled', false).text(originalText);

            if (response.success) {
                showNotification(response.message, 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('resetPasswordModal'));
                modal.hide();
            } else {
                showNotification(response.message, 'error');
            }
        })
        .fail(function (xhr) {
            submitBtn.prop('disabled', false).text(originalText);
            const errorMsg = xhr.responseJSON?.message || 'An error occurred while resetting password.';
            showNotification(errorMsg, 'error');
        });
}

// Utility functions
function showNotification(message, type = 'info', duration = 5000) {
    const alertClass = getAlertClass(type);
    const icon = getAlertIcon(type);

    const alertHtml = `
        <div class="alert ${alertClass} alert-dismissible fade show notification-alert" role="alert">
            <i class="fas ${icon}"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    // Remove existing notifications
    $('.notification-alert').remove();

    // Add new notification
    $('.user-management-container').prepend(alertHtml);

    // Auto-hide after duration
    setTimeout(() => {
        $('.notification-alert').fadeOut(400, function () {
            $(this).remove();
        });
    }, duration);
}

function getAlertClass(type) {
    switch (type) {
        case 'success': return 'alert-success';
        case 'error': return 'alert-danger';
        case 'warning': return 'alert-warning';
        default: return 'alert-info';
    }
}

function getAlertIcon(type) {
    switch (type) {
        case 'success': return 'fa-check-circle';
        case 'error': return 'fa-exclamation-circle';
        case 'warning': return 'fa-exclamation-triangle';
        default: return 'fa-info-circle';
    }
}

function showConfirmDialog(message, onConfirm, onCancel = null) {
    if (confirm(message)) {
        onConfirm();
    } else if (onCancel) {
        onCancel();
    }
}

function showLoadingOverlay() {
    if ($('.loading-overlay').length === 0) {
        const overlay = `
            <div class="loading-overlay">
                <div class="loading-spinner">
                    <i class="fas fa-spinner fa-spin fa-2x"></i>
                    <p>Processing...</p>
                </div>
            </div>
        `;
        $('body').append(overlay);
    }
    $('.loading-overlay').fadeIn(200);
}

function hideLoadingOverlay() {
    $('.loading-overlay').fadeOut(200);
}

// Export functionality
function exportUsers(format = 'csv') {
    showLoadingOverlay();

    window.location.href = `/UserManagement/Export?format=${format}`;

    // Hide loading after a delay (since we can't track file download completion)
    setTimeout(() => {
        hideLoadingOverlay();
        showNotification('Export started. The file will download shortly.', 'info');
    }, 1000);
}

// Statistics loading
function loadUserStats() {
    $.get('/UserManagement/GetUserStats')
        .done(function (response) {
            if (response.success) {
                animateStatsNumbers(response.data);
            }
        })
        .fail(function () {
            console.warn('Failed to load user statistics');
        });
}

function animateStatsNumbers(stats) {
    // Animate numbers with counting effect
    animateNumber('#totalUsers', stats.totalUsers);
    animateNumber('#activeUsers', stats.activeUsers);
    animateNumber('#adminUsers', stats.adminUsers);
    animateNumber('#organizerUsers', stats.organizerUsers);
}

function animateNumber(selector, targetNumber) {
    const element = $(selector);
    const startNumber = 0;
    const duration = 1000;
    const increment = targetNumber / (duration / 16); // 60 FPS

    let currentNumber = startNumber;
    const timer = setInterval(() => {
        currentNumber += increment;
        if (currentNumber >= targetNumber) {
            currentNumber = targetNumber;
            clearInterval(timer);
        }
        element.text(Math.floor(currentNumber));
    }, 16);
}

// Responsive table enhancements
function initTableEnhancements() {
    // Add mobile-friendly table headers
    $('.users-table tbody tr').each(function () {
        $(this).find('td').each(function (index) {
            const headerText = $('.users-table thead th').eq(index).text().trim();
            $(this).attr('data-label', headerText);
        });
    });

    // Add row hover effects with debounce
    let hoverTimeout;
    $('.users-table tbody tr').hover(
        function () {
            clearTimeout(hoverTimeout);
            $(this).addClass('hover-highlight');
        },
        function () {
            const row = $(this);
            hoverTimeout = setTimeout(() => {
                row.removeClass('hover-highlight');
            }, 100);
        }
    );
}

// Advanced search functionality
function initAdvancedSearch() {
    const searchInput = $('.search-input');
    const searchSuggestions = $('<div class="search-suggestions"></div>');

    // Add suggestions container
    searchInput.parent().append(searchSuggestions);

    let searchCache = {};
    let suggestionTimeout;

    searchInput.on('input', function () {
        const query = $(this).val().toLowerCase();

        clearTimeout(suggestionTimeout);

        if (query.length >= 2) {
            suggestionTimeout = setTimeout(() => {
                showSearchSuggestions(query);
            }, 300);
        } else {
            hideSearchSuggestions();
        }
    });

    // Hide suggestions when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.search-box').length) {
            hideSearchSuggestions();
        }
    });
}

function showSearchSuggestions(query) {
    const suggestions = [
        'Active users',
        'Inactive users',
        'Admin users',
        'Event organizers',
        'Recent registrations',
        'Unverified emails'
    ];

    const filteredSuggestions = suggestions.filter(s =>
        s.toLowerCase().includes(query)
    );

    const suggestionsHtml = filteredSuggestions.map(suggestion =>
        `<div class="suggestion-item">${suggestion}</div>`
    ).join('');

    if (suggestionsHtml) {
        $('.search-suggestions').html(suggestionsHtml).show();

        // Handle suggestion clicks
        $('.suggestion-item').on('click', function () {
            const selectedSuggestion = $(this).text();
            $('.search-input').val(selectedSuggestion);
            hideSearchSuggestions();
            $('.filters-form').submit();
        });
    } else {
        hideSearchSuggestions();
    }
}

function hideSearchSuggestions() {
    $('.search-suggestions').hide().empty();
}

// User activity tracking
function trackUserActivity(action, details = {}) {
    // This would typically send activity data to the server
    const activityData = {
        action: action,
        details: details,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
        url: window.location.href
    };

    // In a real implementation, this would be an AJAX call to log the activity
    console.log('User Activity:', activityData);
}

// Keyboard navigation for tables
function initKeyboardNavigation() {
    let focusedRow = -1;
    const tableRows = $('.users-table tbody tr');

    $(document).on('keydown', function (e) {
        if (!$('.search-input').is(':focus') && tableRows.length > 0) {
            switch (e.key) {
                case 'ArrowDown':
                    e.preventDefault();
                    focusedRow = Math.min(focusedRow + 1, tableRows.length - 1);
                    highlightRow(focusedRow);
                    break;

                case 'ArrowUp':
                    e.preventDefault();
                    focusedRow = Math.max(focusedRow - 1, 0);
                    highlightRow(focusedRow);
                    break;

                case 'Enter':
                    if (focusedRow >= 0) {
                        e.preventDefault();
                        const userId = tableRows.eq(focusedRow).data('user-id');
                        window.location.href = `/UserManagement/Details/${userId}`;
                    }
                    break;

                case ' ':
                    if (focusedRow >= 0) {
                        e.preventDefault();
                        const checkbox = tableRows.eq(focusedRow).find('.user-checkbox');
                        checkbox.prop('checked', !checkbox.prop('checked')).trigger('change');
                    }
                    break;
            }
        }
    });
}

function highlightRow(index) {
    $('.users-table tbody tr').removeClass('keyboard-focused');
    if (index >= 0) {
        $('.users-table tbody tr').eq(index).addClass('keyboard-focused');
        // Scroll the row into view if needed
        $('.users-table tbody tr').eq(index)[0].scrollIntoView({
            behavior: 'smooth',
            block: 'nearest'
        });
    }
}

// Data refresh functionality
function initDataRefresh() {
    let refreshInterval;
    const refreshButton = $('<button class="btn btn-outline-secondary btn-sm refresh-btn" title="Refresh Data">' +
        '<i class="fas fa-sync-alt"></i></button>');

    $('.page-actions').prepend(refreshButton);

    refreshButton.on('click', function () {
        refreshData();
    });

    // Auto-refresh every 5 minutes (optional)
    // refreshInterval = setInterval(refreshData, 300000);

    // Clear interval on page unload
    $(window).on('beforeunload', function () {
        if (refreshInterval) {
            clearInterval(refreshInterval);
        }
    });
}

function refreshData() {
    const refreshBtn = $('.refresh-btn');
    const originalIcon = refreshBtn.find('i').attr('class');

    refreshBtn.prop('disabled', true);
    refreshBtn.find('i').attr('class', 'fas fa-spinner fa-spin');

    // Refresh user statistics
    loadUserStats();

    // In a real implementation, you might refresh the table data via AJAX
    setTimeout(() => {
        refreshBtn.prop('disabled', false);
        refreshBtn.find('i').attr('class', originalIcon);
        showNotification('Data refreshed successfully', 'success', 2000);
    }, 1000);
}

// Form validation enhancements
function initFormValidation() {
    // Real-time validation for forms
    $('.form-control').on('blur', function () {
        validateField($(this));
    });

    $('.form-control').on('input', function () {
        // Clear validation errors on input
        $(this).removeClass('is-invalid');
        $(this).siblings('.invalid-feedback').remove();
    });
}

function validateField(field) {
    const value = field.val().trim();
    const fieldType = field.attr('type');
    const isRequired = field.prop('required');

    let isValid = true;
    let errorMessage = '';

    // Required field validation
    if (isRequired && !value) {
        isValid = false;
        errorMessage = 'This field is required.';
    }

    // Email validation
    if (fieldType === 'email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address.';
        }
    }

    // Password validation
    if (fieldType === 'password' && value && value.length < 6) {
        isValid = false;
        errorMessage = 'Password must be at least 6 characters long.';
    }

    // Phone validation
    if (field.attr('name') === 'PhoneNumber' && value) {
        const phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
        if (!phoneRegex.test(value.replace(/[\s\-\(\)]/g, ''))) {
            isValid = false;
            errorMessage = 'Please enter a valid phone number.';
        }
    }

    // Apply validation styles
    if (!isValid) {
        field.addClass('is-invalid');
        field.siblings('.invalid-feedback').remove();
        field.after(`<div class="invalid-feedback">${errorMessage}</div>`);
    } else {
        field.removeClass('is-invalid');
        field.siblings('.invalid-feedback').remove();
    }

    return isValid;
}

// Accessibility enhancements
function initAccessibilityFeatures() {
    // Add ARIA labels and descriptions
    $('.btn-action').each(function () {
        const title = $(this).attr('title');
        if (title) {
            $(this).attr('aria-label', title);
        }
    });

    // Add role attributes
    $('.users-table').attr('role', 'table');
    $('.users-table thead').attr('role', 'rowgroup');
    $('.users-table tbody').attr('role', 'rowgroup');
    $('.users-table tr').attr('role', 'row');
    $('.users-table th').attr('role', 'columnheader');
    $('.users-table td').attr('role', 'cell');

    // Announce bulk selection changes
    $('.user-checkbox').on('change', function () {
        const selectedCount = $('.user-checkbox:checked').length;
        const announcement = `${selectedCount} users selected`;
        announceToScreenReader(announcement);
    });

    // Focus management for modals
    $('#resetPasswordModal').on('shown.bs.modal', function () {
        $('#newPassword').focus();
    });
}

function announceToScreenReader(message) {
    const announcement = $('<div class="sr-only" aria-live="polite" aria-atomic="true"></div>');
    announcement.text(message);
    $('body').append(announcement);

    setTimeout(() => {
        announcement.remove();
    }, 1000);
}

// Performance optimization
function initPerformanceOptimizations() {
    // Lazy load user avatars
    $('.user-avatar').each(function () {
        const avatar = $(this);
        const userId = avatar.closest('tr').data('user-id');

        // This would typically load the actual user avatar image
        // For now, we'll just ensure the initials are properly displayed
        const initials = avatar.text().trim();
        if (initials.length === 0) {
            avatar.text('??');
        }
    });

    // Throttle scroll events
    let scrollTimeout;
    $(window).on('scroll', function () {
        if (!scrollTimeout) {
            scrollTimeout = setTimeout(() => {
                // Handle scroll-based functionality here
                scrollTimeout = null;
            }, 16); // ~60fps
        }
    });

    // Debounce resize events
    let resizeTimeout;
    $(window).on('resize', function () {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
            handleWindowResize();
        }, 250);
    });
}

function handleWindowResize() {
    // Update responsive elements
    updateScrollIndicators();

    // Adjust table layout if needed
    const tableContainer = $('.table-container');
    if ($(window).width() < 768) {
        tableContainer.addClass('mobile-layout');
    } else {
        tableContainer.removeClass('mobile-layout');
    }
}

// Error handling
function initErrorHandling() {
    // Global AJAX error handler
    $(document).ajaxError(function (event, xhr, settings) {
        let errorMessage = 'An unexpected error occurred.';

        if (xhr.status === 403) {
            errorMessage = 'You do not have permission to perform this action.';
        } else if (xhr.status === 404) {
            errorMessage = 'The requested resource was not found.';
        } else if (xhr.status === 500) {
            errorMessage = 'A server error occurred. Please try again later.';
        } else if (xhr.responseJSON && xhr.responseJSON.message) {
            errorMessage = xhr.responseJSON.message;
        }

        showNotification(errorMessage, 'error');
        hideLoadingOverlay();
    });

    // Handle JavaScript errors
    window.addEventListener('error', function (e) {
        console.error('JavaScript Error:', e.error);
        // Don't show notifications for JS errors in production
        // showNotification('A client-side error occurred.', 'error');
    });
}

// Initialize all features when document is ready
$(document).ready(function () {
    initTableEnhancements();
    initAdvancedSearch();
    initKeyboardNavigation();
    initDataRefresh();
    initFormValidation();
    initAccessibilityFeatures();
    initPerformanceOptimizations();
    initErrorHandling();

    // Track page view
    trackUserActivity('page_view', {
        page: 'user_management',
        userCount: $('.users-table tbody tr').length
    });
});

// Export functions for use in HTML
window.userManagement = {
    bulkAction: bulkAction,
    toggleUserStatus: toggleUserStatus,
    showResetPasswordModal: showResetPasswordModal,
    resetPassword: resetPassword,
    exportUsers: exportUsers,
    loadUserStats: loadUserStats,
    showNotification: showNotification
};