// Global variables
let ticketCategoryIndex = 0;

// Document ready function
$(document).ready(function () {
    initializeEventManagement();
});

/**
 * Initialize all event management functionality
 */
function initializeEventManagement() {
    initializeTicketCategories();
    initializeStatusUpdates();
    initializeDeleteConfirmations();
    initializeFormValidation();
    initializeDateValidation();
    initializeImagePreview();
    initializeSearchFunctionality();
    initializeTooltips();
    initializeModals();
    initializeSidebar();
}

/**
 * Initialize ticket category management
 */
function initializeTicketCategories() {
    // Add new ticket category
    $('#addTicketCategory').on('click', function () {
        addTicketCategory();
    });

    // Remove ticket category
    $(document).on('click', '.remove-category', function () {
        removeTicketCategory($(this));
    });

    // Auto-calculate available quantity
    $(document).on('input', 'input[name*="TotalQuantity"]', function () {
        const container = $(this).closest('.ticket-category-item');
        const totalQuantity = parseInt($(this).val()) || 0;
        const availableQuantityInput = container.find('input[name*="AvailableQuantity"]');

        if (availableQuantityInput.val() === '' || availableQuantityInput.prop('readonly')) {
            availableQuantityInput.val(totalQuantity);
        }
    });

    // Price formatting
    $(document).on('input', 'input[name*="Price"]', function () {
        formatPriceInput($(this));
    });
}

/**
 * Add a new ticket category
 */
function addTicketCategory() {
    const template = $('#ticketCategoryTemplate').html();
    const categoryHtml = template.replace(/\[0\]/g, `[${ticketCategoryIndex}]`)
        .replace(/data-index="0"/g, `data-index="${ticketCategoryIndex}"`);

    const categoryElement = $(categoryHtml);
    categoryElement.addClass('fade-in');
    $('#ticketCategoriesContainer').append(categoryElement);

    // Focus on category name input
    categoryElement.find('input[name*="CategoryName"]').focus();

    ticketCategoryIndex++;

    // Show success message
    showNotification('Ticket category added successfully!', 'success');
}

/**
 * Remove a ticket category
 */
function removeTicketCategory(button) {
    const categoryItem = button.closest('.ticket-category-item');

    // Show confirmation
    if (confirm('Are you sure you want to remove this ticket category?')) {
        categoryItem.fadeOut(300, function () {
            $(this).remove();
            reindexTicketCategories();
        });

        showNotification('Ticket category removed successfully!', 'info');
    }
}

/**
 * Reindex ticket categories after removal
 */
function reindexTicketCategories() {
    $('.ticket-category-item').each(function (index) {
        const container = $(this);
        container.attr('data-index', index);

        // Update all input names and IDs
        container.find('input, textarea, select').each(function () {
            const element = $(this);
            const name = element.attr('name');
            if (name) {
                const newName = name.replace(/\[\d+\]/, `[${index}]`);
                element.attr('name', newName);
            }
        });
    });
}

/**
 * Initialize status update functionality
 */
function initializeStatusUpdates() {
    $(document).on('click', '.update-status-btn', function () {
        const eventId = $(this).data('event-id');
        const status = $(this).data('status');
        const statusText = getStatusText(status);

        if (confirm(`Are you sure you want to change the event status to "${statusText}"?`)) {
            updateEventStatus(eventId, status);
        }
    });
}

/**
 * Update event status
 */
function updateEventStatus(eventId, status) {
    const button = $(`.update-status-btn[data-event-id="${eventId}"][data-status="${status}"]`);
    const originalText = button.html();

    // Show loading state
    button.html('<i class="fas fa-spinner fa-spin"></i> Updating...').prop('disabled', true);

    $.ajax({
        url: '/EventManagement/UpdateStatus',
        type: 'POST',
        data: {
            eventId: eventId,
            status: status,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                showNotification(response.message, 'success');

                // Update the status badge in the table/details
                updateStatusBadge(eventId, status);

                // Reload the page after a short delay
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showNotification(response.message || 'Failed to update status', 'error');
                button.html(originalText).prop('disabled', false);
            }
        },
        error: function () {
            showNotification('An error occurred while updating the status', 'error');
            button.html(originalText).prop('disabled', false);
        }
    });
}

/**
 * Get status text based on status code
 */
function getStatusText(status) {
    switch (parseInt(status)) {
        case 0: return 'Draft';
        case 1: return 'Published';
        case 2: return 'Cancelled';
        default: return 'Unknown';
    }
}

/**
 * Update status badge in UI
 */
function updateStatusBadge(eventId, status) {
    const statusText = getStatusText(status).toLowerCase();
    const badge = $(`.status-badge[data-event-id="${eventId}"]`);
    badge.text(getStatusText(status))
        .removeClass('status-draft status-published status-cancelled')
        .addClass(`status-${statusText}`);
}

/**
 * Initialize delete confirmation functionality
 */
function initializeDeleteConfirmations() {
    $(document).on('click', '.delete-event-btn', function () {
        const eventId = $(this).data('event-id');

        if (confirm('Are you sure you want to delete this event? This action cannot be undone.')) {
            deleteEvent(eventId);
        }
    });
}

/**
 * Delete an event
 */
function deleteEvent(eventId) {
    const button = $(`.delete-event-btn[data-event-id="${eventId}"]`);
    const originalText = button.html();

    button.html('<i class="fas fa-spinner fa-spin"></i> Deleting...').prop('disabled', true);

    $.ajax({
        url: '/EventManagement/Delete',
        type: 'POST',
        data: {
            eventId: eventId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                showNotification(response.message, 'success');
                $(`.event-row[data-event-id="${eventId}"]`).fadeOut(300, function () {
                    $(this).remove();
                    // Redirect to index if on details page
                    if (window.location.pathname.includes('/Details')) {
                        setTimeout(() => {
                            window.location.href = '/EventManagement/Index';
                        }, 1500);
                    }
                });
            } else {
                showNotification(response.message || 'Failed to delete event', 'error');
                button.html(originalText).prop('disabled', false);
            }
        },
        error: function () {
            showNotification('An error occurred while deleting the event', 'error');
            button.html(originalText).prop('disabled', false);
        }
    });
}

/**
 * Initialize form validation
 */
function initializeFormValidation() {
    $('#createEventForm').validate({
        rules: {
            EventName: {
                required: true,
                minlength: 3,
                maxlength: 100
            },
            EventDate: {
                required: true
            },
            CategoryId: {
                required: true
            },
            VenueId: {
                required: true
            },
            OrganizerId: {
                required: true
            },
            ImageUrl: {
                url: true
            },
            'TicketCategories[0].CategoryName': {
                required: true,
                minlength: 2,
                maxlength: 50
            },
            'TicketCategories[0].Price': {
                required: true,
                number: true,
                min: 0
            },
            'TicketCategories[0].TotalQuantity': {
                required: true,
                number: true,
                min: 1
            }
        },
        messages: {
            EventName: {
                required: 'Event name is required',
                minlength: 'Event name must be at least 3 characters',
                maxlength: 'Event name cannot exceed 100 characters'
            },
            EventDate: {
                required: 'Event date is required'
            },
            CategoryId: {
                required: 'Please select an event category'
            },
            VenueId: {
                required: 'Please select a venue'
            },
            OrganizerId: {
                required: 'Please select an organizer'
            },
            ImageUrl: {
                url: 'Please enter a valid URL'
            }
        },
        errorPlacement: function (error, element) {
            error.appendTo(element.closest('.col-12, .col-md-6').find('.text-danger'));
        },
        highlight: function (element) {
            $(element).addClass('is-invalid');
        },
        unhighlight: function (element) {
            $(element).removeClass('is-invalid');
        },
        submitHandler: function (form) {
            showNotification('Submitting event data...', 'info');
            form.submit();
        }
    });
}

/**
 * Initialize date validation
 */
function initializeDateValidation() {
    const eventDateInput = $('input[name="EventDate"]');
    const endDateInput = $('input[name="EndDate"]');

    eventDateInput.on('change', function () {
        const eventDate = new Date($(this).val());
        const endDate = endDateInput.val() ? new Date(endDateInput.val()) : null;

        // Ensure event date is not in the past
        if (eventDate < new Date()) {
            showNotification('Event date cannot be in the past', 'error');
            $(this).val('');
        }

        // Validate end date if set
        if (endDate && endDate < eventDate) {
            showNotification('End date cannot be before start date', 'error');
            endDateInput.val('');
        }
    });

    endDateInput.on('change', function () {
        const endDate = new Date($(this).val());
        const eventDate = new Date(eventDateInput.val());

        if (endDate < eventDate) {
            showNotification('End date cannot be before start date', 'error');
            $(this).val('');
        }
    });
}

/**
 * Initialize image preview
 */
function initializeImagePreview() {
    $('input[name="ImageUrl"]').on('input', function () {
        const url = $(this).val();
        const previewContainer = $('.event-image-section');

        if (url) {
            // Check if URL is valid
            if (/^https?:\/\/.*\.(?:png|jpg|jpeg|gif)$/.test(url)) {
                previewContainer.html(`<img src="${url}" alt="Event Preview" class="event-image" />`);
            } else {
                previewContainer.html(`
                    <div class="event-image-placeholder">
                        <i class="fas fa-exclamation-circle"></i>
                        <p>Invalid image URL</p>
                    </div>
                `);
            }
        } else {
            previewContainer.html(`
                <div class="event-image-placeholder">
                    <i class="fas fa-calendar-alt"></i>
                    <p>No Image Available</p>
                </div>
            `);
        }
    });
}

/**
 * Initialize search functionality
 */
function initializeSearchFunctionality() {
    const searchInput = $('input[name="searchTerm"]');

    // Debounce search input
    let searchTimeout;
    searchInput.on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            $('form.filters-form').submit();
        }, 500);
    });

    // Clear search on empty
    searchInput.on('change', function () {
        if (!$(this).val()) {
            $('form.filters-form').submit();
        }
    });
}

/**
 * Initialize tooltips
 */
function initializeTooltips() {
    $('[title]').tooltip({
        placement: 'top',
        trigger: 'hover'
    });
}

/**
 * Initialize modals
 */
function initializeModals() {
    // Create a modal container if it doesn't exist
    if (!$('#eventManagementModal').length) {
        $('body').append(`
            <div class="modal fade" id="eventManagementModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title"></h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body"></div>
                        <div class="modal-footer"></div>
                    </div>
                </div>
            </div>
        `);
    }

    // Example modal trigger (can be customized based on needs)
    $(document).on('click', '.modal-trigger', function () {
        const modal = $('#eventManagementModal');
        modal.find('.modal-title').text($(this).data('title') || 'Event Management');
        modal.find('.modal-body').html($(this).data('content') || '');
        modal.modal('show');
    });
}

/**
 * Initialize sidebar functionality
 */
function initializeSidebar() {
    // Toggle sidebar if it exists
    const sidebarToggle = $('.sidebar-toggle');
    const sidebar = $('.sidebar');

    if (sidebarToggle.length && sidebar.length) {
        sidebarToggle.on('click', function () {
            sidebar.toggleClass('active');
            $('body').toggleClass('sidebar-active');
        });
    }
}

/**
 * Format price input
 */
function formatPriceInput(input) {
    let value = input.val();
    if (value) {
        // Remove non-numeric characters except decimal point
        value = value.replace(/[^0-9.]/g, '');
        // Ensure only one decimal point
        const parts = value.split('.');
        if (parts.length > 2) {
            value = parts[0] + '.' + parts.slice(1).join('');
        }
        // Limit to 2 decimal places
        if (parts[1] && parts[1].length > 2) {
            value = parts[0] + '.' + parts[1].substring(0, 2);
        }
        input.val(value);
    }
}

/**
 * Show notification
 */
function showNotification(message, type) {
    // Remove existing notifications
    $('.notification').remove();

    // Create notification element
    const notification = $(`
        <div class="notification alert alert-${type} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `);

    // Add to body
    $('body').append(notification);

    // Auto-dismiss after 3 seconds
    setTimeout(() => {
        notification.alert('close');
    }, 3000);
}